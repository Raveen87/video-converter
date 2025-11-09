using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal sealed class DefaultFfmpegArgumentsBuilder : IFfmpegArgumentsBuilder
{
    public IReadOnlyList<string> Build(Conversion conversion, string outputFile, string videoTitle, ILogger logger, FfmpegOptions options)
    {
        return FfmpegArgumentsBuilder.Build(conversion, outputFile, videoTitle, logger, options);
    }
}
