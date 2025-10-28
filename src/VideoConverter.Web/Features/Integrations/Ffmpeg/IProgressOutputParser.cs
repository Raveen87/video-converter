using System.Text.RegularExpressions;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IProgressOutputParser
{
    bool TryParseTimeSeconds(string line, out double seconds);
}

public sealed class FfmpegProgressOutputParser : IProgressOutputParser
{
    private static readonly Regex TimeRegex = new(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})", RegexOptions.Compiled);

    public bool TryParseTimeSeconds(string line, out double seconds)
    {
        seconds = 0;
        var match = TimeRegex.Match(line ?? string.Empty);
        if (!match.Success)
        {
            return false;
        }

        var hours = int.Parse(match.Groups[1].Value);
        var minutes = int.Parse(match.Groups[2].Value);
        var secs = int.Parse(match.Groups[3].Value);
        var centiseconds = int.Parse(match.Groups[4].Value);
        seconds = (hours * 3600) + (minutes * 60) + secs + (centiseconds / 100.0);
        return true;
    }
}


