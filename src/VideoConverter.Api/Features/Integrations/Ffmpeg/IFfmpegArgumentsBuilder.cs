using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public interface IFfmpegArgumentsBuilder
{
    IReadOnlyList<string> Build(Conversion conversion, string outputFile, string videoTitle, ILogger logger, FfmpegOptions options);
}
