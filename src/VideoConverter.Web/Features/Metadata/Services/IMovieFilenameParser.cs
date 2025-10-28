using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public interface IMovieFilenameParser
{
    bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer);
}
