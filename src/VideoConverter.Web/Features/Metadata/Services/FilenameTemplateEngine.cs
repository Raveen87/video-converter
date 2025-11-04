using System.Globalization;
using System.Text.RegularExpressions;

using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

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

            return (values.TryGetValue(name, out var raw) && raw is not null) switch
            {
                // Mark empty directives with a sentinel for smart collapse
                false => EmptyDirectiveMarker.ToString(),
                _ => raw is IFormattable formattable && !string.IsNullOrEmpty(format)
                    ? formattable.ToString(format, CultureInfo.InvariantCulture)
                    : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        });
    }

    private static string SanitizePart(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var sanitized = IllegalPathCharactersRegex().Replace(text, "_");
        sanitized = RepeatingSpacesRegex().Replace(sanitized, " ");
        return sanitized.Trim(' ', '.');
    }

    private static string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return "Unknown";
        }

        var sanitized = IllegalPathCharactersRegex().Replace(filename, "_");
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
        var sb = new System.Text.StringBuilder(input.Length);
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
        var sb = new System.Text.StringBuilder(input.Length);
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

    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)(?::([^\}]+))?\}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TokenWithFormatRegex();

    [GeneratedRegex(@"[<>:\""/\\|?*]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IllegalPathCharactersRegex();

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RepeatingSpacesRegex();
}
