using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public interface IFilenameGenerator
{
    string GenerateFilename(MediaMetadata metadata, string originalExtension);
}
