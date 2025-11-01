using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class AudioArgsBuilder
{
    public static IReadOnlyList<string> BuildAudioCodecArgs(Conversion conversion, FfmpegOptions options)
    {
        var args = new List<string>
        {
            "-c:a", options.AudioCodec,
            "-b:a", (conversion.AudioBitrateKbps?.ToString() ?? options.DefaultAudioBitrateKbps.ToString()) + "k",
            "-ac", conversion.AudioChannels?.GetFfmpegChannelLayout() ?? "stereo",
        };
        return args;
    }

    public static void MapAudioStreams(List<string> args, string? audioIndicesCsv, ref int insertAt)
    {
        if (!string.IsNullOrWhiteSpace(audioIndicesCsv))
        {
            var parts = audioIndicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p, out var idx))
                {
                    args.Insert(insertAt, $"0:{idx}");
                    args.Insert(insertAt, "-map");
                    insertAt += 2;
                }
            }
        }
        else
        {
            args.Insert(insertAt, "0:a?");
            args.Insert(insertAt, "-map");
        }
    }
}
