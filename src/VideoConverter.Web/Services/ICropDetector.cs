namespace VideoConverter.Web.Services;

public interface ICropDetector
{
    Task<string?> DetectAsync(string inputPath, double? durationSeconds, CancellationToken cancellationToken);
}


