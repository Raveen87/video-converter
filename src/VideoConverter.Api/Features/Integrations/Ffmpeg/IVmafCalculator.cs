namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Calculates VMAF score by comparing reference and distorted videos.
/// </summary>
public interface IVmafCalculator
{
    /// <summary>
    /// Calculates VMAF score comparing reference video to distorted video.
    /// </summary>
    /// <param name="referencePath">Path to the original source video file.</param>
    /// <param name="distortedPath">Path to the converted/encoded video file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The VMAF score (0-100), or null if calculation fails.</returns>
    Task<double?> CalculateAsync(string referencePath, string distortedPath, CancellationToken ct);
}
