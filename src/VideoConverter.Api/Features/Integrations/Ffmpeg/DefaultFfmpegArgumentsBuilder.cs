using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal sealed class DefaultFfmpegArgumentsBuilder : IFfmpegArgumentsBuilder
{
    public IReadOnlyList<string> Build(Conversion conversion, string outputFile, string videoTitle, ILogger logger, FfmpegOptions options)
    {
        var (targetWidth, targetHeight) = VideoArgsBuilder.GetTargetDimensions(conversion.Resolution);
        logger.LogDebug("Target resolution: {Width}x{Height}", targetWidth, targetHeight);

        var vf = VideoArgsBuilder.BuildVideoFilters(conversion.DetectedCrop, targetWidth, targetHeight, logger);

        var args = new List<string>
        {
            "-y",
            "-i", conversion.InputPath!,
        };

        args.AddRange(VideoArgsBuilder.BuildVideoCodecArgs(conversion, vf, options, logger));
        args.AddRange(AudioArgsBuilder.BuildAudioCodecArgs(conversion, options));
        args.Add(outputFile);

        var externalSubtitlePaths = SubtitleArgsBuilder.ParseExternalSubtitlePaths(conversion.SelectedExternalSubtitlePathsCsv);
        SubtitleArgsBuilder.AddExternalSubtitleInputs(args, externalSubtitlePaths);

        var insertAt = args.IndexOf("-c:v");
        SubtitleArgsBuilder.MapSubtitleStreams(args, conversion.SelectedSubtitleStreamIndicesCsv, insertAt);
        SubtitleArgsBuilder.MapExternalSubtitleStreams(args, externalSubtitlePaths, ref insertAt);
        SubtitleArgsBuilder.AddExternalSubtitleLanguages(args, externalSubtitlePaths, ref insertAt, logger);

        var (metadataArgsBeforeAudio, metadataArgsAfterAudio) = MetadataArgsBuilder.BuildMetadataArgsSplit(videoTitle);
        foreach (var arg in metadataArgsBeforeAudio)
        {
            args.Insert(insertAt, arg);
            insertAt++;
        }

        AudioArgsBuilder.MapAudioStreams(args, conversion.SelectedAudioStreamIndicesCsv, ref insertAt);

        foreach (var arg in metadataArgsAfterAudio)
        {
            args.Insert(insertAt, arg);
            insertAt++;
        }

        return args;
    }
}
