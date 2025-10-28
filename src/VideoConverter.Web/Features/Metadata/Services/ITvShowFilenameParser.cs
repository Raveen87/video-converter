using System.Text.RegularExpressions;
using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public interface ITvShowFilenameParser
{
    bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer);
}

public sealed class TvShowFilenameParser : ITvShowFilenameParser
{
    private static readonly Regex TvShowPattern = new(
        @"^(?<show>.*?)[\s\.\-_]+(?:S(?<season>\d{1,2})E(?<episode>\d{1,2})|(?<season>\d{1,2})x(?<episode>\d{1,2}))[\s\.\-_]*(?<episodeTitle>.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer)
    {
        parsed = default!;
        var tvMatch = TvShowPattern.Match(filenameWithoutExtension);
        if (!tvMatch.Success)
        {
            return false;
        }

        var showName = sanitizer.CleanTitle(tvMatch.Groups["show"].Value);
        var season = int.Parse(tvMatch.Groups["season"].Value);
        var episode = int.Parse(tvMatch.Groups["episode"].Value);
        var episodeTitle = sanitizer.CleanEpisodeTitle(tvMatch.Groups["episodeTitle"].Value);
        if (string.IsNullOrWhiteSpace(episodeTitle))
        {
            episodeTitle = null;
        }

        parsed = new ParsedFilename(
            filenameWithoutExtension,
            MediaType.TvShow,
            showName,
            null,
            season,
            episode,
            episodeTitle,
            source
        );
        return true;
    }
}


