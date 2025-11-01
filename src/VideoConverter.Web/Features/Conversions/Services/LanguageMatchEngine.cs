using System.Text.RegularExpressions;

using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Services;

internal static partial class LanguageMatchEngine
{
    private static readonly Dictionary<string, string> FullNameToIso3 = BuildFullNameToIso3();
    private static readonly Dictionary<string, string[]> Aliases = BuildAliases();

    public static IReadOnlyList<int> Match(
        IReadOnlyList<MediaStream> allStreams,
        IReadOnlyList<string> requestedLanguages,
        Func<MediaStream, bool> streamFilter,
        ILogger logger,
        string kind)
    {
        logger.LogDebug("Matching {Kind} streams for languages: {RequestedLanguages}", kind, string.Join(", ", requestedLanguages));

        var streams = allStreams.Where(streamFilter).ToList();
        logger.LogDebug("Found {StreamCount} {Kind} streams", streams.Count, kind);

        var matched = new List<int>();
        foreach (var req in requestedLanguages)
        {
            var reqKey = Normalize(req);
            var candidates = GetCandidateTokens(reqKey);
            logger.LogDebug(
                "Searching for {Kind} language '{RequestedLanguage}' with candidates: {Candidates}",
                kind,
                req,
                string.Join(", ", candidates));

            var found = streams.Find(s => MatchesStream(s, candidates));
            if (found is null)
            {
                logger.LogWarning("{Kind} language '{RequestedLanguage}' not found in available streams", kind, req);
                throw new InvalidOperationException($"Requested {kind} language '{req}' not found in input file.");
            }

            logger.LogDebug(
                "Matched {Kind} language '{RequestedLanguage}' to stream index {StreamIndex}",
                kind,
                req,
                found.Index);
            matched.Add(found.Index);
        }

        logger.LogInformation("{Kind} language matching completed: {MatchedIndices}", kind, string.Join(", ", matched));
        return matched;
    }

    private static bool MatchesStream(MediaStream stream, HashSet<string> candidates)
    {
        if (!string.IsNullOrWhiteSpace(stream.LanguageTag))
        {
            var tag = Normalize(stream.LanguageTag);
            if (candidates.Contains(tag))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(stream.Title))
        {
            var title = Normalize(stream.Title!);
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

        // Fallback: if the requested token is itself an alias (e.g., "sv"),
        // include the entire alias set for the language whose aliases contain it.
        foreach (var pair in Aliases)
        {
            var aliasList = pair.Value;
            if (aliasList.Contains(req, StringComparer.Ordinal))
            {
                foreach (var a in aliasList)
                {
                    _ = tokens.Add(a);
                }

                if (FullNameToIso3.TryGetValue(pair.Key, out var iso))
                {
                    _ = tokens.Add(iso);
                }
            }
        }

        return tokens;
    }

    private static string Normalize(string s)
    {
        var lower = s.Trim().ToLowerInvariant();
        lower = EverythingExceptEnglishLettersRegex().Replace(lower, string.Empty);
        return lower;
    }

    private static Dictionary<string, string> BuildFullNameToIso3()
    {
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
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Normalize("english"), ["en", "eng", "english"] },
            { Normalize("swedish"), ["sv", "sve", "swe", "svenska", "swedish"] },
            { Normalize("vietnamese"), ["vi", "vie", "viet", "vietnamese"] },
            { Normalize("german"), ["de", "deu", "ger", "deutsch"] },
            { Normalize("french"), ["fr", "fra", "fre", "francais"] },
            { Normalize("spanish"), ["es", "spa", "esp", "espanol"] },
            { Normalize("italian"), ["it", "ita", "italiano"] },
            { Normalize("japanese"), ["ja", "jpn", "nihongo"] },
            { Normalize("chinese"), ["zh", "zho", "chi", "han"] },
            { Normalize("korean"), ["ko", "kor", "hangul"] },
            { Normalize("norwegian"), ["no", "nor", "norsk"] },
            { Normalize("danish"), ["da", "dan", "dansk"] },
            { Normalize("finnish"), ["fi", "fin", "suomi"] },
        };
    }

    [GeneratedRegex("[^a-z]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EverythingExceptEnglishLettersRegex();
}
