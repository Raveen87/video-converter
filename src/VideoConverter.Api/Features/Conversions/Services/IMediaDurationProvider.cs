namespace VideoConverter.Api.Features.Conversions.Services;

public interface IMediaDurationProvider
{
    Task<double> GetDurationSecondsAsync(string inputPath, CancellationToken cancellationToken);
}
