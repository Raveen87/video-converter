namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class GrainDetectionClassifier
{
    public static GrainLevel Classify(double medianPsnr, double lowPsnrThreshold, double highPsnrThreshold)
    {
        if (double.IsNaN(medianPsnr))
        {
            return GrainLevel.Low;
        }

        var safeHigh = Math.Max(0, highPsnrThreshold);
        var safeLow = Math.Max(safeHigh, lowPsnrThreshold);

        if (medianPsnr <= safeHigh)
        {
            return GrainLevel.High;
        }

        return medianPsnr <= safeLow
            ? GrainLevel.Low
            : GrainLevel.None;
    }
}
