using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public interface ISubtitleLanguageMatcher
{
    /// <summary>
    /// Given subtitle streams and requested languages (full names), returns the set of matching stream indices.
    /// Throws InvalidOperationException if any requested language has no match.
    /// </summary>
    IReadOnlyList<int> MatchSubtitleStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages);
}



