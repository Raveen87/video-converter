using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public sealed partial class FilenameTemplateEngine : IFilenameTemplateEngine
{
    private const char EmptyDirectiveMarker = '\u0001'; // non-printable sentinel

    public string Render(MediaMetadata metadata, string originalExtension, string template)
    {
        var extension = string.IsNullOrWhiteSpace(originalExtension) ? ".mp4" : originalExtension;
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        var values = BuildTokenValues(metadata);

        // Replace directives; mark empty ones, then smart-collapse adjacent glue/affixes
        var renderedWithMarkers = ReplaceTokens(template, values);
        renderedWithMarkers = CollapseEmptyDirectives(renderedWithMarkers);

        // Fix edge-case: ensure separator before Source when EpisodeName/Season/Episode collapse removed it
        renderedWithMarkers = FixMissingDotBeforeSource(renderedWithMarkers, template, values);

        var rendered = SanitizeFilename(renderedWithMarkers);
        return rendered + extension;
    }

    private static Dictionary<string, object?> BuildTokenValues(MediaMetadata metadata)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        switch (metadata)
        {
            case MovieMetadata movie:
                dict["Title"] = SanitizePart(movie.Title);
                dict["Year"] = movie.Year;
                dict["Season"] = null;
                dict["Episode"] = null;
                dict["EpisodeName"] = null;
                dict["Source"] = SanitizePart(movie.Source);
                break;
            case TvShowMetadata tv:
                dict["Title"] = SanitizePart(tv.ShowName);
                dict["Year"] = tv.FirstAiredYear;
                dict["Season"] = tv.Season;
                dict["Episode"] = tv.Episode;
                dict["EpisodeName"] = SanitizePart(tv.EpisodeTitle);
                dict["Source"] = SanitizePart(tv.Source);
                break;
            default:
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
                // Mark empty directives with a sentinel for smart collapse
                return EmptyDirectiveMarker.ToString();
            }

            // Apply string filters for string tokens (e.g., spaces=dot, lower, upper, trim)
            if (raw is string str)
            {
                return ApplyStringFilters(str, format);
            }

            // Preserve numeric formatting for numeric tokens
            return raw is IFormattable formattable && !string.IsNullOrEmpty(format)
                ? formattable.ToString(format, CultureInfo.InvariantCulture)
                : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;
        });
    }

    private static string SanitizePart(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Always enforce ASCII
        var ascii = ToAscii(text);
        var sanitized = IllegalPathCharactersRegex().Replace(ascii, "_");
        sanitized = RepeatingSpacesRegex().Replace(sanitized, " ");
        return sanitized.Trim(' ', '.');
    }

    private static string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return "Unknown";
        }

        // Always enforce ASCII
        var ascii = ToAscii(filename);
        var sanitized = IllegalPathCharactersRegex().Replace(ascii, "_");
        sanitized = RepeatingSpacesRegex().Replace(sanitized, " ");
        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized.Trim(' ', '.');
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
        return finalStr.Trim(' ', '.', '-', '_');
    }

    private static bool IsSeparator(char ch)
    {
        return ch is ' ' or '.' or '-' or '_';
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
                // Remove left-side separators (they belong to the empty directive)
                while (sb.Length > 0 && IsSeparator(sb[^1]))
                {
                    _ = sb.Remove(sb.Length - 1, 1);
                }

                // Skip marker and lookahead right-side separators
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

                if (preservedSep != '\0')
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

    private static string FixMissingDotBeforeSource(string input, string template, IReadOnlyDictionary<string, object?> values)
    {
        // Apply only when template expresses ".{EpisodeName}.{Source}" and EpisodeName is empty while Source has a value
        if (!template.Contains(".{EpisodeName}.{Source}", StringComparison.Ordinal))
        {
            return input;
        }

        _ = values.TryGetValue("EpisodeName", out var epRaw);
        var episodeName = Convert.ToString(epRaw, CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(episodeName))
        {
            return input;
        }

        if (!values.TryGetValue("Source", out var srcRaw) || srcRaw is null)
        {
            return input;
        }

        var source = Convert.ToString(srcRaw, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(source))
        {
            return input;
        }

        var idx = input.IndexOf(source, StringComparison.Ordinal);
        if (idx <= 0)
        {
            return input;
        }

        var prev = input[idx - 1];
        return prev is '.' or '-' or '_' or ' ' ? input : input.Insert(idx, ".");
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

    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)(?::([^\}]+))?\}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TokenWithFormatRegex();

    [GeneratedRegex(@"[<>:\""/\\|?*]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IllegalPathCharactersRegex();

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RepeatingSpacesRegex();
}
