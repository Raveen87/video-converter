namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public interface IGrainDetector
{
    Task<GrainLevel> DetectAsync(string inputPath, double? durationSeconds, CancellationToken cancellationToken);
}
