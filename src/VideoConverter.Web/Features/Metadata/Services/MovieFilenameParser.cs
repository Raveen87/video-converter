using System.Text.RegularExpressions;

using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed partial class MovieFilenameParser : IMovieFilenameParser
{
    private static readonly Regex MoviePattern = MovieRegex();

    private static readonly Regex YearPattern = YearRegex();

    public bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer)
    {
        var movieMatch = MoviePattern.Match(filenameWithoutExtension);
        if (movieMatch.Success)
        {
            var year = int.Parse(movieMatch.Groups["year"].Value);
            if (year is >= 1900 and <= 2030)
            {
                var title = sanitizer.CleanTitle(movieMatch.Groups["title"].Value);
                parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, title, year, Season: null, Episode: null, EpisodeTitle: null, source);
                return true;
            }

            var fullTitle = sanitizer.CleanTitlePreservingDots(movieMatch.Groups[0].Value);
            parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, fullTitle, Year: null, Season: null, Episode: null, EpisodeTitle: null, source);
            return true;
        }

        var yearMatch = YearPattern.Match(filenameWithoutExtension);
        if (yearMatch.Success)
        {
            var year = int.Parse(yearMatch.Value);
            if (year is >= 1900 and <= 2030)
            {
                var title = sanitizer.CleanTitle(filenameWithoutExtension.Replace(yearMatch.Value, "", StringComparison.Ordinal));
                parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, title, year, Season: null, Episode: null, EpisodeTitle: null, source);
                return true;
            }
        }

        parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, sanitizer.CleanTitle(filenameWithoutExtension), Year: null, Season: null, Episode: null, EpisodeTitle: null, source);
        return true;
    }

    [GeneratedRegex(@"^(?<title>.*?)[\s\.\-_]+\(?(?<year>\d{4})\)?[\s\.\-_]*(?:\.|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MovieRegex();

    [GeneratedRegex(@"\b(19|20)\d{2}\b", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex YearRegex();
}
