using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class FfmpegArgumentsBuilder
{
    public static List<string> Build(
        Conversion conversion,
        string outputFile,
        string videoTitle,
        ILogger logger)
    {
        var (targetWidth, targetHeight) = VideoArgsBuilder.GetTargetDimensions(conversion.Resolution);
        logger.LogDebug("Target resolution: {Width}x{Height}", targetWidth, targetHeight);

        var vf = VideoArgsBuilder.BuildVideoFilters(conversion.DetectedCrop, targetWidth, targetHeight, logger);

        var args = new List<string>
        {
            "-y",
            "-i", conversion.InputPath!
        };

        args.AddRange(VideoArgsBuilder.BuildVideoCodecArgs(conversion, vf));
        args.AddRange(AudioArgsBuilder.BuildAudioCodecArgs(conversion));
        args.Add(outputFile);

        var externalSubtitlePaths = SubtitleArgsBuilder.ParseExternalSubtitlePaths(conversion.SelectedExternalSubtitlePathsCsv);
        SubtitleArgsBuilder.AddExternalSubtitleInputs(args, externalSubtitlePaths);

        var insertAt = args.IndexOf("-c:v");
        SubtitleArgsBuilder.MapSubtitleStreams(args, conversion.SelectedSubtitleStreamIndicesCsv, insertAt);
        SubtitleArgsBuilder.MapExternalSubtitleStreams(args, externalSubtitlePaths, ref insertAt);
        SubtitleArgsBuilder.AddExternalSubtitleLanguages(args, externalSubtitlePaths, ref insertAt, logger);

        MetadataArgsBuilder.AddSubtitleCodecCopy(args, ref insertAt);
        MetadataArgsBuilder.AddTitleMetadata(args, videoTitle, ref insertAt);
        AudioArgsBuilder.MapAudioStreams(args, conversion.SelectedAudioStreamIndicesCsv, ref insertAt);
        MetadataArgsBuilder.PreserveInputMetadata(args, ref insertAt);

        return args;
    }
}


