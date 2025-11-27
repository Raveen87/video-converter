using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Services;

internal static partial class LanguageMatchEngine
{
    private static readonly Dictionary<string, string> FullNameToIso3 = BuildFullNameToIso3();
    private static readonly Dictionary<string, string[]> Aliases = BuildAliases();

    public static IReadOnlyList<string> GetAliasesForLanguage(string languageName)
    {
        var normalized = Normalize(languageName);
        if (Aliases.TryGetValue(normalized, out var aliases))
        {
            return aliases;
        }

        var matchingPair = Aliases.FirstOrDefault(pair => pair.Value.Contains(normalized, StringComparer.Ordinal));
        if (matchingPair.Value is not null)
        {
            return matchingPair.Value;
        }

        return [normalized];
    }

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
            var (language, isOptional) = ParseLanguageRequest(req);
            var reqKey = Normalize(language);
            var candidates = GetCandidateTokens(reqKey);
            logger.LogDebug(
                "Searching for {Kind} language '{RequestedLanguage}' (optional: {IsOptional}) with candidates: {Candidates}",
                kind,
                language,
                isOptional,
                string.Join(", ", candidates));

            var found = streams.Find(s => MatchesStream(s, candidates));
            if (found is null)
            {
                if (isOptional)
                {
                    logger.LogInformation("{Kind} language '{RequestedLanguage}' not found in available streams (optional, skipping)", kind, language);
                    continue;
                }

                logger.LogWarning("{Kind} language '{RequestedLanguage}' not found in available streams", kind, language);
                throw ApiErrorException.ValidationError(
                    errorMessage: $"Requested {kind} language '{language}' not found in input file.",
                    userFriendlyMessage: $"The requested {kind} language '{language}' was not found in the input file.");
            }

            logger.LogDebug(
                "Matched {Kind} language '{RequestedLanguage}' to stream index {StreamIndex}",
                kind,
                language,
                found.Index);
            matched.Add(found.Index);
        }

        logger.LogInformation("{Kind} language matching completed: {MatchedIndices}", kind, string.Join(", ", matched));
        return matched;
    }

    private static (string Language, bool IsOptional) ParseLanguageRequest(string request)
    {
        var trimmed = request.Trim();
        if (trimmed.EndsWith('?'))
        {
            return (trimmed[..^1].TrimEnd(), true);
        }

        return (trimmed, false);
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
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        AddMajorLanguages(dict);
        AddEuropeanLanguages(dict);
        AddAsianLanguages(dict);
        AddAfricanLanguages(dict);
        AddOtherLanguages(dict);
        return dict;
    }

    private static void AddMajorLanguages(Dictionary<string, string> dict)
    {
        AddTop20Languages(dict);
        AddTop21To40Languages(dict);
        AddTop41To60Languages(dict);
    }

    private static void AddTop20Languages(Dictionary<string, string> dict)
    {
        dict[Normalize("english")] = "eng";
        dict[Normalize("mandarin")] = "cmn";
        dict[Normalize("chinese")] = "zho";
        dict[Normalize("hindi")] = "hin";
        dict[Normalize("spanish")] = "spa";
        dict[Normalize("french")] = "fra";
        dict[Normalize("arabic")] = "ara";
        dict[Normalize("bengali")] = "ben";
        dict[Normalize("portuguese")] = "por";
        dict[Normalize("russian")] = "rus";
        dict[Normalize("urdu")] = "urd";
        dict[Normalize("indonesian")] = "ind";
        dict[Normalize("german")] = "deu";
        dict[Normalize("japanese")] = "jpn";
        dict[Normalize("swahili")] = "swa";
        dict[Normalize("marathi")] = "mar";
        dict[Normalize("telugu")] = "tel";
        dict[Normalize("turkish")] = "tur";
        dict[Normalize("tamil")] = "tam";
        dict[Normalize("cantonese")] = "yue";
    }

    private static void AddTop21To40Languages(Dictionary<string, string> dict)
    {
        dict[Normalize("vietnamese")] = "vie";
        dict[Normalize("tagalog")] = "tgl";
        dict[Normalize("korean")] = "kor";
        dict[Normalize("persian")] = "fas";
        dict[Normalize("farsi")] = "fas";
        dict[Normalize("hausa")] = "hau";
        dict[Normalize("italian")] = "ita";
        dict[Normalize("gujarati")] = "guj";
        dict[Normalize("thai")] = "tha";
        dict[Normalize("amharic")] = "amh";
        dict[Normalize("bhojpuri")] = "bho";
        dict[Normalize("kannada")] = "kan";
        dict[Normalize("yoruba")] = "yor";
        dict[Normalize("burmese")] = "mya";
        dict[Normalize("polish")] = "pol";
        dict[Normalize("malayalam")] = "mal";
        dict[Normalize("odia")] = "ori";
        dict[Normalize("punjabi")] = "pan";
        dict[Normalize("sundanese")] = "sun";
        dict[Normalize("igbo")] = "ibo";
    }

    private static void AddTop41To60Languages(Dictionary<string, string> dict)
    {
        dict[Normalize("uzbek")] = "uzb";
        dict[Normalize("sindhi")] = "snd";
        dict[Normalize("ukrainian")] = "ukr";
        dict[Normalize("romanian")] = "ron";
        dict[Normalize("dutch")] = "nld";
        dict[Normalize("khmer")] = "khm";
        dict[Normalize("somali")] = "som";
        dict[Normalize("malay")] = "zsm";
        dict[Normalize("cebuano")] = "ceb";
        dict[Normalize("nepali")] = "nep";
        dict[Normalize("assamese")] = "asm";
        dict[Normalize("sinhala")] = "sin";
        dict[Normalize("sinhalese")] = "sin";
        dict[Normalize("kurdish")] = "kur";
        dict[Normalize("azerbaijani")] = "aze";
    }

    private static void AddEuropeanLanguages(Dictionary<string, string> dict)
    {
        dict[Normalize("swedish")] = "swe";
        dict[Normalize("norwegian")] = "nor";
        dict[Normalize("danish")] = "dan";
        dict[Normalize("finnish")] = "fin";
        dict[Normalize("greek")] = "ell";
        dict[Normalize("hebrew")] = "heb";
        dict[Normalize("czech")] = "ces";
        dict[Normalize("hungarian")] = "hun";
        dict[Normalize("bulgarian")] = "bul";
        dict[Normalize("croatian")] = "hrv";
        dict[Normalize("serbian")] = "srp";
        dict[Normalize("slovak")] = "slk";
        dict[Normalize("slovenian")] = "slv";
        dict[Normalize("lithuanian")] = "lit";
        dict[Normalize("latvian")] = "lav";
        dict[Normalize("estonian")] = "est";
        dict[Normalize("icelandic")] = "isl";
        dict[Normalize("maltese")] = "mlt";
        dict[Normalize("albanian")] = "sqi";
        dict[Normalize("macedonian")] = "mkd";
        dict[Normalize("bosnian")] = "bos";
        dict[Normalize("belarusian")] = "bel";
        dict[Normalize("georgian")] = "kat";
        dict[Normalize("armenian")] = "hye";
    }

    private static void AddAsianLanguages(Dictionary<string, string> dict)
    {
        dict[Normalize("kazakh")] = "kaz";
        dict[Normalize("kyrgyz")] = "kir";
        dict[Normalize("tajik")] = "tgk";
        dict[Normalize("turkmen")] = "tuk";
        dict[Normalize("mongolian")] = "mon";
        dict[Normalize("pashto")] = "pus";
        dict[Normalize("javanese")] = "jav";
        dict[Normalize("lao")] = "lao";
        dict[Normalize("ilocano")] = "ilo";
        dict[Normalize("waray")] = "war";
        dict[Normalize("bikol")] = "bcl";
        dict[Normalize("kapampangan")] = "pam";
        dict[Normalize("pangasinan")] = "pag";
        dict[Normalize("shanghainese")] = "wuu";
        dict[Normalize("wu chinese")] = "wuu";
        dict[Normalize("hokkien")] = "nan";
        dict[Normalize("min nan")] = "nan";
        dict[Normalize("jin chinese")] = "cjy";
        dict[Normalize("xiang chinese")] = "hsn";
        dict[Normalize("gan chinese")] = "gan";
    }

    private static void AddAfricanLanguages(Dictionary<string, string> dict)
    {
        dict[Normalize("afrikaans")] = "afr";
        dict[Normalize("zulu")] = "zul";
        dict[Normalize("xhosa")] = "xho";
    }

    private static void AddOtherLanguages(Dictionary<string, string> dict)
    {
        dict[Normalize("haryanvi")] = "bgc";
        dict[Normalize("rajasthani")] = "raj";
        dict[Normalize("magahi")] = "mag";
        dict[Normalize("chhattisgarhi")] = "hne";
        dict[Normalize("fula")] = "ful";
        dict[Normalize("fulfulde")] = "ful";
        dict[Normalize("nigerian pidgin")] = "pcm";
    }

    private static Dictionary<string, string[]> BuildAliases()
    {
        var dict = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddMajorLanguageAliases(dict);
        AddEuropeanLanguageAliases(dict);
        AddAsianLanguageAliases(dict);
        AddAfricanLanguageAliases(dict);
        AddOtherLanguageAliases(dict);
        return dict;
    }

    private static void AddMajorLanguageAliases(Dictionary<string, string[]> dict)
    {
        AddTop20LanguageAliases(dict);
        AddTop21To40LanguageAliases(dict);
        AddTop41To60LanguageAliases(dict);
    }

    private static void AddTop20LanguageAliases(Dictionary<string, string[]> dict)
    {
        dict[Normalize("english")] = ["en", "eng", "english"];
        dict[Normalize("mandarin")] = ["cmn", "zh", "zho", "mandarin", "chinese", "putonghua", "guoyu"];
        dict[Normalize("chinese")] = ["zh", "zho", "chi", "han", "chinese", "mandarin", "cmn"];
        dict[Normalize("hindi")] = ["hi", "hin", "hindi"];
        dict[Normalize("spanish")] = ["es", "spa", "esp", "espanol", "spanish", "castellano"];
        dict[Normalize("french")] = ["fr", "fra", "fre", "francais", "french", "français"];
        dict[Normalize("arabic")] = ["ar", "ara", "arabic", "arab", "العربية"];
        dict[Normalize("bengali")] = ["bn", "ben", "bengali", "bangla", "বাংলা"];
        dict[Normalize("portuguese")] = ["pt", "por", "portuguese", "português"];
        dict[Normalize("russian")] = ["ru", "rus", "russian", "русский"];
        dict[Normalize("urdu")] = ["ur", "urd", "urdu", "اردو"];
        dict[Normalize("indonesian")] = ["id", "ind", "indonesian", "bahasa indonesia"];
        dict[Normalize("german")] = ["de", "deu", "ger", "german", "deutsch"];
        dict[Normalize("japanese")] = ["ja", "jpn", "japanese", "nihongo", "日本語"];
        dict[Normalize("swahili")] = ["sw", "swa", "swahili", "kiswahili"];
        dict[Normalize("marathi")] = ["mr", "mar", "marathi", "मराठी"];
        dict[Normalize("telugu")] = ["te", "tel", "telugu", "తెలుగు"];
        dict[Normalize("turkish")] = ["tr", "tur", "turkish", "türkçe"];
        dict[Normalize("tamil")] = ["ta", "tam", "tamil", "தமிழ்"];
        dict[Normalize("cantonese")] = ["yue", "zh", "cantonese", "粵語", "yueyu"];
    }

    private static void AddTop21To40LanguageAliases(Dictionary<string, string[]> dict)
    {
        dict[Normalize("vietnamese")] = ["vi", "vie", "viet", "vietnamese", "tiếng việt"];
        dict[Normalize("tagalog")] = ["tl", "tgl", "tagalog", "filipino", "fil"];
        dict[Normalize("korean")] = ["ko", "kor", "korean", "한국어", "hangul"];
        dict[Normalize("persian")] = ["fa", "fas", "per", "persian", "farsi", "فارسی"];
        dict[Normalize("farsi")] = ["fa", "fas", "per", "persian", "farsi", "فارسی"];
        dict[Normalize("hausa")] = ["ha", "hau", "hausa"];
        dict[Normalize("italian")] = ["it", "ita", "italian", "italiano"];
        dict[Normalize("gujarati")] = ["gu", "guj", "gujarati", "ગુજરાતી"];
        dict[Normalize("thai")] = ["th", "tha", "thai", "ไทย"];
        dict[Normalize("amharic")] = ["am", "amh", "amharic", "አማርኛ"];
        dict[Normalize("bhojpuri")] = ["bho", "bhojpuri"];
        dict[Normalize("kannada")] = ["kn", "kan", "kannada", "ಕನ್ನಡ"];
        dict[Normalize("yoruba")] = ["yo", "yor", "yoruba"];
        dict[Normalize("burmese")] = ["my", "mya", "bur", "burmese", "မြန်မာ"];
        dict[Normalize("polish")] = ["pl", "pol", "polish", "polski"];
        dict[Normalize("malayalam")] = ["ml", "mal", "malayalam", "മലയാളം"];
        dict[Normalize("odia")] = ["or", "ori", "odia", "odia", "ଓଡ଼ିଆ"];
        dict[Normalize("punjabi")] = ["pa", "pan", "punjabi", "ਪੰਜਾਬੀ", "پنجابی"];
        dict[Normalize("sundanese")] = ["su", "sun", "sundanese", "basa sunda"];
        dict[Normalize("igbo")] = ["ig", "ibo", "igbo"];
    }

    private static void AddTop41To60LanguageAliases(Dictionary<string, string[]> dict)
    {
        dict[Normalize("uzbek")] = ["uz", "uzb", "uzbek", "oʻzbek"];
        dict[Normalize("sindhi")] = ["sd", "snd", "sindhi", "سنڌي"];
        dict[Normalize("ukrainian")] = ["uk", "ukr", "ukrainian", "українська"];
        dict[Normalize("romanian")] = ["ro", "ron", "rum", "romanian", "română"];
        dict[Normalize("dutch")] = ["nl", "nld", "dut", "dutch", "nederlands"];
        dict[Normalize("khmer")] = ["km", "khm", "khmer", "ភាសាខ្មែរ"];
        dict[Normalize("somali")] = ["so", "som", "somali", "af soomaali"];
        dict[Normalize("malay")] = ["ms", "zsm", "may", "malay", "bahasa melayu"];
        dict[Normalize("cebuano")] = ["ceb", "cebuano", "bisaya"];
        dict[Normalize("nepali")] = ["ne", "nep", "nepali", "नेपाली"];
        dict[Normalize("assamese")] = ["as", "asm", "assamese", "অসমীয়া"];
        dict[Normalize("sinhala")] = ["si", "sin", "sinhala", "sinhalese", "සිංහල"];
        dict[Normalize("sinhalese")] = ["si", "sin", "sinhala", "sinhalese", "සිංහල"];
        dict[Normalize("kurdish")] = ["ku", "kur", "kurdish", "کوردی"];
        dict[Normalize("azerbaijani")] = ["az", "aze", "azerbaijani", "azərbaycan"];
    }

    private static void AddEuropeanLanguageAliases(Dictionary<string, string[]> dict)
    {
        dict[Normalize("swedish")] = ["sv", "sve", "swe", "svenska", "swedish"];
        dict[Normalize("norwegian")] = ["no", "nor", "norsk", "norwegian", "nynorsk", "bokmål"];
        dict[Normalize("danish")] = ["da", "dan", "dansk", "danish"];
        dict[Normalize("finnish")] = ["fi", "fin", "suomi", "finnish"];
        dict[Normalize("greek")] = ["el", "ell", "gre", "greek", "ελληνικά"];
        dict[Normalize("hebrew")] = ["he", "heb", "hebrew", "עברית"];
        dict[Normalize("czech")] = ["cs", "ces", "cze", "czech", "čeština"];
        dict[Normalize("hungarian")] = ["hu", "hun", "hungarian", "magyar"];
        dict[Normalize("bulgarian")] = ["bg", "bul", "bulgarian", "български"];
        dict[Normalize("croatian")] = ["hr", "hrv", "croatian", "hrvatski"];
        dict[Normalize("serbian")] = ["sr", "srp", "serbian", "српски"];
        dict[Normalize("slovak")] = ["sk", "slk", "slo", "slovak", "slovenčina"];
        dict[Normalize("slovenian")] = ["sl", "slv", "slovenian", "slovenščina"];
        dict[Normalize("lithuanian")] = ["lt", "lit", "lithuanian", "lietuvių"];
        dict[Normalize("latvian")] = ["lv", "lav", "latvian", "latviešu"];
        dict[Normalize("estonian")] = ["et", "est", "estonian", "eesti"];
        dict[Normalize("icelandic")] = ["is", "isl", "ice", "icelandic", "íslenska"];
        dict[Normalize("maltese")] = ["mt", "mlt", "maltese", "malti"];
        dict[Normalize("albanian")] = ["sq", "sqi", "alb", "albanian", "shqip"];
        dict[Normalize("macedonian")] = ["mk", "mkd", "mac", "macedonian", "македонски"];
        dict[Normalize("bosnian")] = ["bs", "bos", "bosnian", "bosanski"];
        dict[Normalize("belarusian")] = ["be", "bel", "belarusian", "беларуская"];
        dict[Normalize("georgian")] = ["ka", "kat", "geo", "georgian", "ქართული"];
        dict[Normalize("armenian")] = ["hy", "hye", "arm", "armenian", "հայերեն"];
    }

    private static void AddAsianLanguageAliases(Dictionary<string, string[]> dict)
    {
        dict[Normalize("kazakh")] = ["kk", "kaz", "kazakh", "қазақ"];
        dict[Normalize("kyrgyz")] = ["ky", "kir", "kyrgyz", "кыргызча"];
        dict[Normalize("tajik")] = ["tg", "tgk", "tajik", "тоҷикӣ"];
        dict[Normalize("turkmen")] = ["tk", "tuk", "turkmen", "türkmen"];
        dict[Normalize("mongolian")] = ["mn", "mon", "mongolian", "монгол"];
        dict[Normalize("pashto")] = ["ps", "pus", "pashto", "پښتو"];
        dict[Normalize("javanese")] = ["jv", "jav", "javanese", "basa jawa"];
        dict[Normalize("lao")] = ["lo", "lao", "lao", "ລາວ"];
        dict[Normalize("ilocano")] = ["ilo", "ilocano"];
        dict[Normalize("waray")] = ["war", "waray", "waray-waray"];
        dict[Normalize("bikol")] = ["bcl", "bikol", "bicol"];
        dict[Normalize("kapampangan")] = ["pam", "kapampangan", "pampango"];
        dict[Normalize("pangasinan")] = ["pag", "pangasinan"];
        dict[Normalize("shanghainese")] = ["wuu", "shanghainese", "wu chinese"];
        dict[Normalize("wu chinese")] = ["wuu", "shanghainese", "wu chinese"];
        dict[Normalize("hokkien")] = ["nan", "hokkien", "min nan", "taiwanese"];
        dict[Normalize("min nan")] = ["nan", "hokkien", "min nan", "taiwanese"];
        dict[Normalize("jin chinese")] = ["cjy", "jin chinese", "jin"];
        dict[Normalize("xiang chinese")] = ["hsn", "xiang chinese", "xiang", "hunanese"];
        dict[Normalize("gan chinese")] = ["gan", "gan chinese", "gan"];
    }

    private static void AddAfricanLanguageAliases(Dictionary<string, string[]> dict)
    {
        dict[Normalize("afrikaans")] = ["af", "afr", "afrikaans"];
        dict[Normalize("zulu")] = ["zu", "zul", "zulu", "isi zulu"];
        dict[Normalize("xhosa")] = ["xh", "xho", "xhosa", "isi xhosa"];
    }

    private static void AddOtherLanguageAliases(Dictionary<string, string[]> dict)
    {
        dict[Normalize("haryanvi")] = ["bgc", "haryanvi"];
        dict[Normalize("rajasthani")] = ["raj", "rajasthani"];
        dict[Normalize("magahi")] = ["mag", "magahi"];
        dict[Normalize("chhattisgarhi")] = ["hne", "chhattisgarhi"];
        dict[Normalize("fula")] = ["ff", "ful", "fula", "fulfulde", "pulaar"];
        dict[Normalize("fulfulde")] = ["ff", "ful", "fula", "fulfulde", "pulaar"];
        dict[Normalize("nigerian pidgin")] = ["pcm", "nigerian pidgin", "naija"];
    }

    [GeneratedRegex("[^a-z]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EverythingExceptEnglishLettersRegex();
}
