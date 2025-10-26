namespace VideoConverter.Web.Services.Ffmpeg;

public interface ICropDetector
{
    Task<string?> DetectAsync(string inputPath, double? durationSeconds, CancellationToken cancellationToken);
}


