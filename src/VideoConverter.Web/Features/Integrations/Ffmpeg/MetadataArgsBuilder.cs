namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class MetadataArgsBuilder
{
    public static void AddSubtitleCodecCopy(List<string> args, ref int insertAt)
    {
        args.Insert(insertAt, "copy");
        args.Insert(insertAt, "-c:s");
        insertAt += 2;
    }

    public static void AddTitleMetadata(List<string> args, string videoTitle, ref int insertAt)
    {
        args.Insert(insertAt, "-metadata");
        args.Insert(insertAt, "title=" + videoTitle);
        insertAt += 2;
    }

    public static void PreserveInputMetadata(List<string> args, ref int insertAt)
    {
        args.Insert(insertAt, "-map_metadata");
        args.Insert(insertAt, "0");
    }
}
