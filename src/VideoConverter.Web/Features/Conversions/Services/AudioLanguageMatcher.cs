using System.Text.RegularExpressions;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Services;

public sealed class AudioLanguageMatcher(ILogger<AudioLanguageMatcher> logger) : IAudioLanguageMatcher
{
    private readonly ILogger<AudioLanguageMatcher> _logger = logger;

    public IReadOnlyList<int> MatchAudioStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages)
    {
        return LanguageMatchEngine.Match(streams, requestedLanguages, s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase), _logger, "audio");
    }
}


