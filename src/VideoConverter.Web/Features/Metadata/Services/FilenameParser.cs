using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public class FilenameParser(ISourceDetector sourceDetector, IMovieFilenameParser movieParser, ITvShowFilenameParser tvParser, ITitleSanitizer sanitizer) : IFilenameParser
{
    public FilenameParser()
        : this(new SourceDetector(), new MovieFilenameParser(), new TvShowFilenameParser(), new TitleSanitizer())
    {
    }

    public ParsedFilename ParseFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return new ParsedFilename(filename, MediaType.Movie);
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        var source = sourceDetector.DetectSource(nameWithoutExtension);

        if (tvParser.TryParse(nameWithoutExtension, out var tvParsed, source, sanitizer))
        {
            return tvParsed with { OriginalFilename = filename };
        }

        _ = movieParser.TryParse(nameWithoutExtension, out var movieParsed, source, sanitizer);
        return movieParsed with { OriginalFilename = filename };
    }
}
