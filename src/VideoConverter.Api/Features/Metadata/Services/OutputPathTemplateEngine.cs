using System.Text;

using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

/// <summary>
/// Renders output path templates using media metadata.
/// Supports tokens like {Type}, {Title}, {Year}, {Season}, {Episode}, {EpisodeName}, {Source}.
/// </summary>
public sealed partial class OutputPathTemplateEngine : IOutputPathTemplateEngine
{
    private const char EmptyDirectiveMarker = '\u0001';

    /// <inheritdoc />
    public string Render(MediaMetadata metadata, string template)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var values = BuildTokenValues(metadata);
        var renderedWithMarkers = ReplaceTokens(template, values);
        renderedWithMarkers = CollapseEmptyDirectives(renderedWithMarkers);
        var sanitized = SanitizePath(renderedWithMarkers);
        var normalized = NormalizePathSeparators(sanitized);
        var cleaned = RemoveEmptySegments(normalized);

        return cleaned;
    }

    private static Dictionary<string, object?> BuildTokenValues(MediaMetadata metadata)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        switch (metadata)
        {
            case MovieMetadata movie:
                dict["Type"] = "Movie";
                dict["Title"] = movie.Title;
                dict["Year"] = movie.Year;
                dict["Season"] = null;
                dict["Episode"] = null;
                dict["EpisodeName"] = null;
                dict["Source"] = movie.Source;
                break;

            case TvShowMetadata tv:
                dict["Type"] = "TV";
                dict["Title"] = tv.ShowName;
                dict["Year"] = tv.FirstAiredYear;
                dict["Season"] = tv.Season;
                dict["Episode"] = tv.Episode;
                dict["EpisodeName"] = tv.EpisodeTitle;
                dict["Source"] = tv.Source;
                break;
            default:
                dict["Type"] = "Unknown";
                dict["Title"] = "Unknown";
                break;
        }

        return dict;
    }

    private static string ReplaceTokens(string input, IReadOnlyDictionary<string, object?> values)
    {
        return TokenWithFormatRegex().Replace(input, m =>
        {
            var name = m.Groups[1].Value;
            var format = m.Groups[2].Success ? m.Groups[2].Value : null;

            if (!(values.TryGetValue(name, out var raw) && raw is not null))
            {
                return EmptyDirectiveMarker.ToString();
            }

            if (raw is string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                {
                    return EmptyDirectiveMarker.ToString();
                }

                return ApplyStringFilters(str, format);
            }

            return raw is IFormattable formattable && !string.IsNullOrEmpty(format)
                ? formattable.ToString(format, CultureInfo.InvariantCulture)
                : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;
        });
    }

    private static string ApplyStringFilters(string input, string? formatSpec)
    {
        var value = input;
        if (string.IsNullOrEmpty(formatSpec))
        {
            return value;
        }

        var specItems = formatSpec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        char? spaceReplacement = null;

        foreach (var spec in specItems)
        {
            if (spec.StartsWith("spaces=", StringComparison.OrdinalIgnoreCase))
            {
                var mode = spec["spaces=".Length..].Trim().ToLowerInvariant();
                spaceReplacement = mode switch
                {
                    "dot" => '.',
                    "underscore" => '_',
                    "hyphen" => '-',
                    "space" => ' ',
                    _ => spaceReplacement,
                };
                continue;
            }

            switch (spec.ToLowerInvariant())
            {
                case "lower":
                    value = value.ToLowerInvariant();
                    break;
                case "upper":
                    value = value.ToUpperInvariant();
                    break;
                case "trim":
                    value = value.Trim();
                    break;
                default:
                    break;
            }
        }

        if (spaceReplacement is char replacementChar)
        {
            value = value.Replace(' ', replacementChar);
            value = CollapseRepeatedChar(value, replacementChar);
            value = value.Trim(replacementChar);
        }

        return value;
    }

    private static string CollapseRepeatedChar(string input, char ch)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        var lastWas = false;
        foreach (var c in input)
        {
            if (c == ch)
            {
                if (!lastWas)
                {
                    _ = sb.Append(c);
                    lastWas = true;
                }

                continue;
            }

            lastWas = false;
            _ = sb.Append(c);
        }

        return sb.ToString();
    }

    private static string CollapseEmptyDirectives(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var afterAffixRemoval = RemoveSingleLetterAffixes(input);
        var withoutSeparators = RemoveSeparatorsAroundMarkers(afterAffixRemoval);
        var finalStr = withoutSeparators.Replace(EmptyDirectiveMarker.ToString(), string.Empty, StringComparison.Ordinal);
        return finalStr;
    }

    private static bool IsSeparator(char ch)
    {
        return ch is ' ' or '.' or '-' or '_' or '/' or '\\';
    }

    private static string RemoveSingleLetterAffixes(string input)
    {
        var sb = new StringBuilder(input.Length);
        var i = 0;
        while (i < input.Length)
        {
            var ch = input[i];
            if (char.IsLetter(ch) && i + 1 < input.Length && input[i + 1] == EmptyDirectiveMarker)
            {
                i++;
                continue;
            }

            if (input[i] == EmptyDirectiveMarker && i + 1 < input.Length && char.IsLetter(input[i + 1]))
            {
                _ = sb.Append(EmptyDirectiveMarker);
                i += 2;
                continue;
            }

            _ = sb.Append(ch);
            i++;
        }

        return sb.ToString();
    }

    private static string RemoveSeparatorsAroundMarkers(string input)
    {
        var sb = new StringBuilder(input.Length);
        var i = 0;
        while (i < input.Length)
        {
            var ch = input[i];
            if (ch == EmptyDirectiveMarker)
            {
                while (sb.Length > 0 && IsSeparator(sb[^1]))
                {
                    _ = sb.Remove(sb.Length - 1, 1);
                }

                var preservedSep = '\0';
                var j = i + 1;
                if (j < input.Length && IsSeparator(input[j]))
                {
                    preservedSep = input[j];
                    while (j < input.Length && IsSeparator(input[j]))
                    {
                        j++;
                    }
                }

                i = j;

                var shouldAppendSep = preservedSep is '/' or '\\'
                    || (preservedSep is not '\0' && sb.Length > 0 && !IsSeparator(sb[^1]));

                if (shouldAppendSep)
                {
                    _ = sb.Append(preservedSep);
                }

                continue;
            }

            _ = sb.Append(ch);
            i++;
        }

        return sb.ToString();
    }

    private static string SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var ascii = ToAscii(path);
        var sanitized = IllegalPathCharactersRegex().Replace(ascii, "_");
        sanitized = RepeatingSpacesRegex().Replace(sanitized, " ");
        return sanitized;
    }

    private static string ToAscii(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (c is <= (char)0x7F)
            {
                _ = sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string NormalizePathSeparators(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var separator = Path.DirectorySeparatorChar;
        var result = path.Replace('/', separator).Replace('\\', separator);
        result = CollapseRepeatedChar(result, separator);
        return result;
    }

    private static string RemoveEmptySegments(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var separator = Path.DirectorySeparatorChar;
        var segments = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var cleanedSegments = segments
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        return cleanedSegments is []
            ? string.Empty
            : string.Join(separator, cleanedSegments);
    }

    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)(?::([^\}]+))?\}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TokenWithFormatRegex();

    [GeneratedRegex(@"[<>:""|?*]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IllegalPathCharactersRegex();

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RepeatingSpacesRegex();
}
