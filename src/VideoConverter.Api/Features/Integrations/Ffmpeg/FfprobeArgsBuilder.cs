namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class FfprobeArgsBuilder
{
    public static IReadOnlyList<string> Build(string inputPath)
    {
        return new[]
        {
            "-v", "error",
            "-print_format", "json",
            "-show_streams",
            "-show_format",
            inputPath,
        };
    }
}
