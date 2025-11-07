using VideoConverter.Web.Features.Integrations.Ffmpeg;

namespace VideoConverter.Web.Services.Conversion;

public interface IConversionExecutionService
{
    Task<ConversionStartResult> RunByIdAsync(int id, CancellationToken ct);

    Task<(ConversionStartResult Result, int ConversionId)> RunNextPendingAsync(CancellationToken ct);
}
