using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class SubtitleLanguageMatcher(ILogger<SubtitleLanguageMatcher> logger) : ISubtitleLanguageMatcher
{
    private readonly ILogger<SubtitleLanguageMatcher> _logger = logger;

    public IReadOnlyList<int> MatchSubtitleStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages)
    {
        return LanguageMatchEngine.Match(
            streams,
            requestedLanguages,
            s => string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase)
                && !SubtitleCodecHelpers.IsImageSubtitle(s.CodecName)
                && SubtitleCodecHelpers.IsTextSubtitle(s.CodecName),
            _logger,
            "subtitle");
    }
}
