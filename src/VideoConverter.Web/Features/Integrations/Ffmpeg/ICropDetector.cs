namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface ICropDetector
{
    Task<string?> DetectAsync(string inputPath, double? durationSeconds, CancellationToken cancellationToken);
}
