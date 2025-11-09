using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IFfmpegArgumentsBuilder
{
    IReadOnlyList<string> Build(Conversion conversion, string outputFile, string videoTitle, ILogger logger, FfmpegOptions options);
}
