namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class MetadataArgsBuilder
{
    public static List<string> BuildSubtitleCodecSrtArgs()
    {
        return ["-c:s", "srt"];
    }

    public static List<string> BuildTitleMetadataArgs(string videoTitle)
    {
        return ["-metadata", "title=" + videoTitle];
    }

    public static List<string> BuildPreserveInputMetadataArgs()
    {
        return ["-map_metadata", "0"];
    }

    public static List<string> BuildMetadataArgs(string videoTitle)
    {
        var args = new List<string>();
        args.AddRange(BuildSubtitleCodecSrtArgs());
        args.AddRange(BuildTitleMetadataArgs(videoTitle));
        args.AddRange(BuildPreserveInputMetadataArgs());
        return args;
    }

    public static (List<string> BeforeAudio, List<string> AfterAudio) BuildMetadataArgsSplit(string videoTitle)
    {
        var beforeAudio = new List<string>();
        beforeAudio.AddRange(BuildSubtitleCodecSrtArgs());
        beforeAudio.AddRange(BuildTitleMetadataArgs(videoTitle));

        var afterAudio = BuildPreserveInputMetadataArgs();

        return (beforeAudio, afterAudio);
    }
}
