using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public interface IFilenameGenerator
{
    string GenerateFilename(MediaMetadata metadata, string originalExtension);
}
