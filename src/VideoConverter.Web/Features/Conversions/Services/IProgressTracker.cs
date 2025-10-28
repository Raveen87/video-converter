using VideoConverter.Web.Features.Conversions.Dtos;

namespace VideoConverter.Web.Features.Conversions.Services;

public interface IProgressTracker
{
    void StartTracking(int conversionId, double totalDurationSeconds);

    void UpdateProgress(int conversionId, double currentTimeSeconds);

    void StopTracking(int conversionId);

    ConversionProgress? GetProgress(int conversionId);
}
