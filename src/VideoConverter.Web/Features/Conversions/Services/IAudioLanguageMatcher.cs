using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Services;

public interface IAudioLanguageMatcher
{
    /// <summary>
    /// Given audio streams and requested languages (full names), returns the set of matching stream indices.
    /// Throws InvalidOperationException if any requested language has no match.
    /// </summary>
    IReadOnlyList<int> MatchAudioStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages);
}

