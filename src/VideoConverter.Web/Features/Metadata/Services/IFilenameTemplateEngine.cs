using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public interface IFilenameTemplateEngine
{
    string Render(MediaMetadata metadata, string originalExtension, string template);
}
