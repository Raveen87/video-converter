using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public class FilenameParser(ISourceDetector sourceDetector, IMovieFilenameParser movieParser, ITvShowFilenameParser tvParser, ITitleSanitizer sanitizer) : IFilenameParser
{
    private readonly ISourceDetector _sourceDetector = sourceDetector;
    private readonly IMovieFilenameParser _movieParser = movieParser;
    private readonly ITvShowFilenameParser _tvParser = tvParser;
    private readonly ITitleSanitizer _sanitizer = sanitizer;

    public FilenameParser() : this(new SourceDetector(), new MovieFilenameParser(), new TvShowFilenameParser(), new TitleSanitizer())
    {
    }

    public ParsedFilename ParseFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return new ParsedFilename(filename, MediaType.Movie);
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        var source = _sourceDetector.DetectSource(nameWithoutExtension);

        if (_tvParser.TryParse(nameWithoutExtension, out var tvParsed, source, _sanitizer))
        {
            return tvParsed with { OriginalFilename = filename };
        }

        _ = _movieParser.TryParse(nameWithoutExtension, out var movieParsed, source, _sanitizer);
        return movieParsed with { OriginalFilename = filename };
    }

}
