using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IConversionRunner
{
    Task<ConversionStartResult> StartAsync(Conversion conversion);
}
