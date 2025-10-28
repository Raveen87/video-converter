using System.Text.RegularExpressions;
using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public interface IMovieFilenameParser
{
    bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer);
}

public sealed class MovieFilenameParser : IMovieFilenameParser
{
    private static readonly Regex MoviePattern = new(
        @"^(?<title>.*?)[\s\.\-_]+\(?(?<year>\d{4})\)?[\s\.\-_]*(?:\.|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YearPattern = new(@"\b(19|20)\d{2}\b", RegexOptions.Compiled);

    public bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer)
    {
        var movieMatch = MoviePattern.Match(filenameWithoutExtension);
        if (movieMatch.Success)
        {
            var year = int.Parse(movieMatch.Groups["year"].Value);
            if (year is >= 1900 and <= 2030)
            {
                var title = sanitizer.CleanTitle(movieMatch.Groups["title"].Value);
                parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, title, year, null, null, null, source);
                return true;
            }
            else
            {
                var fullTitle = sanitizer.CleanTitlePreservingDots(movieMatch.Groups[0].Value);
                parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, fullTitle, null, null, null, null, source);
                return true;
            }
        }

        var yearMatch = YearPattern.Match(filenameWithoutExtension);
        if (yearMatch.Success)
        {
            var year = int.Parse(yearMatch.Value);
            if (year is >= 1900 and <= 2030)
            {
                var title = sanitizer.CleanTitle(filenameWithoutExtension.Replace(yearMatch.Value, ""));
                parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, title, year, null, null, null, source);
                return true;
            }
        }

        parsed = new ParsedFilename(filenameWithoutExtension, MediaType.Movie, sanitizer.CleanTitle(filenameWithoutExtension), null, null, null, null, source);
        return true;
    }
}


