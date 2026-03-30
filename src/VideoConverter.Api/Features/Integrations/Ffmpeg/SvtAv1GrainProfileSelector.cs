namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class SvtAv1GrainProfileSelector
{
    public static SvtAv1GrainProfile Select(FfmpegOptions options, GrainLevel detectedGrainLevel)
    {
        var preferredProfile = detectedGrainLevel switch
        {
            GrainLevel.None => options.SvtParamsNoGrain,
            GrainLevel.Low => options.SvtParamsLowGrain,
            GrainLevel.High => options.SvtParamsHighGrain,
            _ => options.SvtParamsLowGrain,
        };

        var usedFallback = string.IsNullOrWhiteSpace(preferredProfile);
        var safeProfile = string.IsNullOrWhiteSpace(preferredProfile)
            ? ResolveLegacyFallback(options)
            : preferredProfile;
        var appliedProfileName = detectedGrainLevel.ToString();
        if (usedFallback)
        {
            appliedProfileName = string.IsNullOrWhiteSpace(safeProfile)
                ? "Disabled"
                : "Legacy";
        }

        return new SvtAv1GrainProfile(appliedProfileName, safeProfile, usedFallback);
    }

    private static string ResolveLegacyFallback(FfmpegOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SvtParams))
        {
            return options.SvtParams;
        }

        return string.Empty;
    }
}
