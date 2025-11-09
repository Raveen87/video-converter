using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Services;

public sealed class SubtitleLanguageMatcher(ILogger<SubtitleLanguageMatcher> logger) : ISubtitleLanguageMatcher
{
    private readonly ILogger<SubtitleLanguageMatcher> _logger = logger;

    public IReadOnlyList<int> MatchSubtitleStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages)
    {
        return LanguageMatchEngine.Match(
            streams,
            requestedLanguages,
            s => string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase) && IsTextSubtitle(s.CodecName),
            _logger,
            "subtitle");
    }

    private static bool IsTextSubtitle(string? codecName)
    {
        if (string.IsNullOrWhiteSpace(codecName))
        {
            return false;
        }

        // ffprobe codec_name values for text subs commonly include:
        // subrip (SRT), ass, ssa, webvtt, mov_text
        // Include "srt" to be defensive if any probes surface that alias.
        return codecName.Equals("subrip", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("srt", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("ass", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("ssa", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("webvtt", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("mov_text", StringComparison.OrdinalIgnoreCase);
    }
}
