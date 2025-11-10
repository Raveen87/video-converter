using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public interface IConversionRunner
{
    Task<ConversionStartResult> StartAsync(Conversion conversion, CancellationToken ct);
}
