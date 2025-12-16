using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

/// <summary>
/// Renders output path templates using media metadata.
/// </summary>
public interface IOutputPathTemplateEngine
{
    /// <summary>
    /// Renders an output path template using the provided metadata.
    /// </summary>
    /// <param name="metadata">Media metadata containing title, year, season, etc.</param>
    /// <param name="template">The output path template string.</param>
    /// <returns>The rendered path with all tokens replaced and sanitized.</returns>
    string Render(MediaMetadata metadata, string template);
}
