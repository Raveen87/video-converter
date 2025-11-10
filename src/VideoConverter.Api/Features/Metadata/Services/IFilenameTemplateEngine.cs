using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public interface IFilenameTemplateEngine
{
    string Render(MediaMetadata metadata, string originalExtension, string template);
}
