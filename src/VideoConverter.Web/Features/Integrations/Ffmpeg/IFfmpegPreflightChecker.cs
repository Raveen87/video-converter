namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IFfmpegPreflightChecker
{
    Task<(bool ok, string? error)> EnsurePreflightAsync(string ffmpegPath, CancellationToken cancellationToken);
}
