using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface ISubtitleLanguageMatcher
{
    /// <summary>
    /// Given subtitle streams and requested languages (full names), returns the set of matching stream indices.
    /// Throws InvalidOperationException if any requested language has no match.
    /// </summary>
    /// <param name="streams">The available subtitle streams.</param>
    /// <param name="requestedLanguages">The requested subtitle languages.</param>
    /// <returns>The list of matching found audio streams.</returns>
    IReadOnlyList<int> MatchSubtitleStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages);
}
