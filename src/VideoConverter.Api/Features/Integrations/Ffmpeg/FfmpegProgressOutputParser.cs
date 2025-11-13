using System.Text.RegularExpressions;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed partial class FfmpegProgressOutputParser(ILogger<FfmpegProgressOutputParser> logger) : IProgressOutputParser
{
    private readonly ILogger<FfmpegProgressOutputParser> _logger = logger;

    public bool TryParseTimeSeconds(string line, out double seconds)
    {
        seconds = 0;
        var match = TimeRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        var hours = int.Parse(match.Groups[1].Value);
        var minutes = int.Parse(match.Groups[2].Value);
        var secs = int.Parse(match.Groups[3].Value);
        var centiseconds = int.Parse(match.Groups[4].Value);
        seconds = (hours * 3600) + (minutes * 60) + secs + (centiseconds / 100.0);

        _logger.LogDebug(
            "Successfully parsed time from FFmpeg output: {TimeString} -> {Seconds}s ({TimeFormatted})",
            match.Value,
            seconds,
            TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss\.ff"));

        return true;
    }

    [GeneratedRegex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TimeRegex();
}
