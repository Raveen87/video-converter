using System.Globalization;
using System.Text.RegularExpressions;

using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed partial class FilenameTemplateEngine : IFilenameTemplateEngine
{
    public string Render(MediaMetadata metadata, string originalExtension, string template)
    {
        var extension = string.IsNullOrWhiteSpace(originalExtension) ? ".mp4" : originalExtension;
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        var values = BuildTokenValues(metadata);

        var rendered = RenderSegments(template, values);
        rendered = ReplaceTokens(rendered, values);

        rendered = SanitizeFilename(rendered);
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

    private static string RenderSegments(string template, IReadOnlyDictionary<string, object?> values)
    {
        return SegmentRegex().Replace(template, m =>
        {
            var segmentContent = m.Groups[1].Value;
            var tokens = TokenWithFormatRegex().Matches(segmentContent)
                .Select(t => t.Groups[1].Value)
                .ToList();

            var include = tokens.Count is 0 || tokens.TrueForAll(t => !IsNullOrEmpty(values, t));
            return include ? ReplaceTokens(segmentContent, values) : string.Empty;
        });
    }

    private static string ReplaceTokens(string input, IReadOnlyDictionary<string, object?> values)
    {
        return TokenWithFormatRegex().Replace(input, m =>
        {
            var name = m.Groups[1].Value;
            var format = m.Groups[2].Success ? m.Groups[2].Value : null;

            return (values.TryGetValue(name, out var raw) && raw is not null) switch
            {
                false => string.Empty,
                _ => raw is IFormattable formattable && !string.IsNullOrEmpty(format)
                    ? formattable.ToString(format, CultureInfo.InvariantCulture)
                    : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        });
    }

    private static bool IsNullOrEmpty(IReadOnlyDictionary<string, object?> values, string name)
    {
        return !values.TryGetValue(name, out var raw) || raw is null || string.IsNullOrWhiteSpace(Convert.ToString(raw, CultureInfo.InvariantCulture));
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

    [GeneratedRegex(@"\[([^\[\]]+)\]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SegmentRegex();

    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)(?::([^\}]+))?\}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TokenWithFormatRegex();

    [GeneratedRegex(@"[<>:\""/\\|?*]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IllegalPathCharactersRegex();

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RepeatingSpacesRegex();
}
