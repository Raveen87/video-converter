using System.Text.RegularExpressions;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Services;

public sealed class SubtitleLanguageMatcher(ILogger<SubtitleLanguageMatcher> logger) : ISubtitleLanguageMatcher
{
    private readonly ILogger<SubtitleLanguageMatcher> _logger = logger;

    public IReadOnlyList<int> MatchSubtitleStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages)
    {
        return LanguageMatchEngine.Match(streams, requestedLanguages, s => string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase), _logger, "subtitle");
    }
}
