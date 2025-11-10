using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class AudioLanguageMatcher(ILogger<AudioLanguageMatcher> logger) : IAudioLanguageMatcher
{
    public IReadOnlyList<int> MatchAudioStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages)
    {
        return LanguageMatchEngine.Match(streams, requestedLanguages, s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase), logger, "audio");
    }
}
