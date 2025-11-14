using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public sealed partial class TvShowFilenameParser : ITvShowFilenameParser
{
    public bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer)
    {
        parsed = default!;
        var tvMatch = TvShowRegex().Match(filenameWithoutExtension);
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
            Year: null,
            season,
            episode,
            episodeTitle,
            source);
        return true;
    }

    [GeneratedRegex(@"^(?<show>.*?)[\s\.\-_]+(?:S(?<season>\d{1,2})E(?<episode>\d{1,2})|(?<season>\d{1,2})x(?<episode>\d{1,2}))[\s\.\-_]*(?<episodeTitle>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TvShowRegex();
}
