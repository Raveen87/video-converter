using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IConversionExecutionService
{
    Task<ConversionStartResult> RunByIdAsync(int id, CancellationToken ct);

    Task<(ConversionStartResult Result, int ConversionId)> RunNextPendingAsync(CancellationToken ct);
}
