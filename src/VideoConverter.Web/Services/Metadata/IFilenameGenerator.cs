using VideoConverter.Web.Domain.Models;

namespace VideoConverter.Web.Services.Metadata;

public interface IFilenameGenerator
{
    string GenerateFilename(MediaMetadata metadata, string originalExtension);
}
