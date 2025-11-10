namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public interface IFfmpegPreflightChecker
{
    Task<(bool Ok, string? Error)> EnsurePreflightAsync(string ffmpegPath, CancellationToken cancellationToken);
}
