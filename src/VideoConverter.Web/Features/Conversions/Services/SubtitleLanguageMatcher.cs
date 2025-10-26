using System.Text.RegularExpressions;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Services;

public sealed class SubtitleLanguageMatcher(ILogger<SubtitleLanguageMatcher> logger) : ISubtitleLanguageMatcher
{
    private static readonly Dictionary<string, string> FullNameToIso3 = BuildFullNameToIso3();
    private static readonly Dictionary<string, string[]> Aliases = BuildAliases();
    private readonly ILogger<SubtitleLanguageMatcher> _logger = logger;

    public IReadOnlyList<int> MatchSubtitleStreams(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> requestedLanguages)
    {
        _logger.LogDebug("Matching subtitle streams for languages: {RequestedLanguages}",
            string.Join(", ", requestedLanguages));

        var subStreams = streams.Where(s => string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase)).ToList();
        _logger.LogDebug("Found {SubtitleStreamCount} subtitle streams", subStreams.Count);

        var matched = new List<int>();
        foreach (var req in requestedLanguages)
        {
            var reqKey = Normalize(req);
            var candidates = GetCandidateTokens(reqKey);
            _logger.LogDebug("Searching for subtitle language '{RequestedLanguage}' with candidates: {Candidates}",
                req, string.Join(", ", candidates));

            var found = subStreams.Find(s => MatchesStream(s, candidates));
            if (found is null)
            {
                _logger.LogWarning("Subtitle language '{RequestedLanguage}' not found in available streams", req);
                throw new InvalidOperationException($"Requested subtitle language '{req}' not found in input file.");
            }

            _logger.LogDebug("Matched subtitle language '{RequestedLanguage}' to stream index {StreamIndex}",
                req, found.Index);
            matched.Add(found.Index);
        }

        _logger.LogInformation("Subtitle language matching completed: {MatchedIndices}", string.Join(", ", matched));
        return matched;
    }

    private static bool MatchesStream(MediaStream stream, HashSet<string> candidates)
    {
        // Try language tag (iso639-2/3 like 'eng', or 2-letter 'en')
        if (!string.IsNullOrWhiteSpace(stream.LanguageTag))
        {
            var tag = Normalize(stream.LanguageTag);
            if (candidates.Contains(tag))
            {
                return true;
            }
        }

        // Try title/name
        if (!string.IsNullOrWhiteSpace(stream.Title))
        {
            var title = Normalize(stream.Title!);
            // split title into tokens, check any contains candidate
            if (candidates.Any(c => title.Contains(c, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> GetCandidateTokens(string req)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        _ = tokens.Add(req);

        if (FullNameToIso3.TryGetValue(req, out var iso3))
        {
            _ = tokens.Add(iso3);
        }
        if (Aliases.TryGetValue(req, out var aliasArr))
        {
            foreach (var a in aliasArr)
            {
                _ = tokens.Add(a);
            }
        }

        return tokens;
    }

    private static string Normalize(string s)
    {
        var lower = s.Trim().ToLowerInvariant();
        lower = Regex.Replace(lower, "[^a-z]", ""); // keep letters only
        return lower;
    }

    private static Dictionary<string, string> BuildFullNameToIso3()
    {
        // Minimal map; can be extended. Keys normalized.
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { Normalize("english"), "eng" },
            { Normalize("swedish"), "swe" },
            { Normalize("vietnamese"), "vie" },
            { Normalize("german"), "deu" },
            { Normalize("french"), "fra" },
            { Normalize("spanish"), "spa" },
            { Normalize("italian"), "ita" },
            { Normalize("japanese"), "jpn" },
            { Normalize("chinese"), "zho" },
            { Normalize("korean"), "kor" },
            { Normalize("norwegian"), "nor" },
            { Normalize("danish"), "dan" },
            { Normalize("finnish"), "fin" },
        };
    }

    private static Dictionary<string, string[]> BuildAliases()
    {
        // Aliases include common abbreviations and two-letter codes
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Normalize("english"), new[] { "en", "eng", "english" } },
            { Normalize("swedish"), new[] { "sv", "sve", "swe", "svenska", "swedish" } },
            { Normalize("vietnamese"), new[] { "vi", "vie", "viet", "vietnamese" } },
            { Normalize("german"), new[] { "de", "deu", "ger", "deutsch" } },
            { Normalize("french"), new[] { "fr", "fra", "fre", "francais" } },
            { Normalize("spanish"), new[] { "es", "spa", "esp", "espanol" } },
            { Normalize("italian"), new[] { "it", "ita", "italiano" } },
            { Normalize("japanese"), new[] { "ja", "jpn", "nihongo" } },
            { Normalize("chinese"), new[] { "zh", "zho", "chi", "han" } },
            { Normalize("korean"), new[] { "ko", "kor", "hangul" } },
            { Normalize("norwegian"), new[] { "no", "nor", "norsk" } },
            { Normalize("danish"), new[] { "da", "dan", "dansk" } },
            { Normalize("finnish"), new[] { "fi", "fin", "suomi" } },
        };
    }
}
