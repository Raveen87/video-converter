using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public interface IFilenameGenerator
{
    string GenerateFilename(MediaMetadata metadata, string originalExtension);
}
