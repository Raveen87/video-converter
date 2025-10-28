using System.Text.RegularExpressions;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed class SourceDetector : ISourceDetector
{
    // Ordered by length, longest first to prefer WEB-DL over WEB
    private static readonly string[] SourcePatterns = [
        "WEB-DL", "DVDRip", "WebRip", "BDRip", "PDTV", "HDTV", "Bluray", "WEB", "TV",
    ];

    public string? DetectSource(string filenameWithoutExtension)
    {
        if (string.IsNullOrWhiteSpace(filenameWithoutExtension))
        {
            return null;
        }

        var filenameLower = filenameWithoutExtension.ToLowerInvariant();
        string? firstMatch = null;
        var firstMatchIndex = int.MaxValue;

        foreach (var pattern in SourcePatterns)
        {
            var patternLower = pattern.ToLowerInvariant();
            var match = Regex.Match(filenameLower, $@"\b{Regex.Escape(patternLower)}\b", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (match.Success && match.Index < firstMatchIndex)
            {
                firstMatch = pattern; // Return the original case
                firstMatchIndex = match.Index;
            }
        }

        return firstMatch;
    }
}
