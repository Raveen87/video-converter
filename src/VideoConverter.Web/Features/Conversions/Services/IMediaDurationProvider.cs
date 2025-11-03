namespace VideoConverter.Web.Features.Conversions.Services;

public interface IMediaDurationProvider
{
    Task<double> GetDurationSecondsAsync(string inputPath, CancellationToken cancellationToken);
}
