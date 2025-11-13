namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class MetadataArgsBuilder
{
    public static void AddSubtitleCodecSrt(List<string> args, ref int insertAt)
    {
        args.Insert(insertAt, "srt");
        args.Insert(insertAt, "-c:s");
        insertAt += 2;
    }

    public static void AddTitleMetadata(List<string> args, string videoTitle, ref int insertAt)
    {
        args.Insert(insertAt, "title=" + videoTitle);
        args.Insert(insertAt, "-metadata");
        insertAt += 2;
    }

    public static void PreserveInputMetadata(List<string> args, ref int insertAt)
    {
        args.Insert(insertAt, "-map_metadata");
        args.Insert(insertAt + 1, "0");
        insertAt += 2;
    }
}
