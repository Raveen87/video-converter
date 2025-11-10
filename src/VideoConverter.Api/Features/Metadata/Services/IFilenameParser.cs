using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public interface IFilenameParser
{
    ParsedFilename ParseFilename(string filename);
}
