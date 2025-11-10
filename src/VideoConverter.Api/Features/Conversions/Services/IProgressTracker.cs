using VideoConverter.Api.Features.Conversions.Dtos;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IProgressTracker
{
    void StartTracking(int conversionId, double totalDurationSeconds);

    void UpdateProgress(int conversionId, double currentTimeSeconds);

    void StopTracking(int conversionId);

    ConversionProgress? GetProgress(int conversionId);
}
