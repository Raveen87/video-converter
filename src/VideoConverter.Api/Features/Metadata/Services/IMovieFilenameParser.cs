using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public interface IMovieFilenameParser
{
    bool TryParse(string filenameWithoutExtension, out ParsedFilename parsed, string? source, ITitleSanitizer sanitizer);
}
