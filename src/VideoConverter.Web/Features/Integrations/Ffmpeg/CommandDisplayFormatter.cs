namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public static class CommandDisplayFormatter
{
    public static string Build(string executablePath, IReadOnlyList<string> arguments)
    {
        static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            var needsQuotes = s.Any(ch => char.IsWhiteSpace(ch) || "&|><^()'\"".Contains(ch));
            return needsQuotes ? "\"" + s.Replace("\"", "\\\"") + "\"" : s;
        }

        var displayArgs = new List<string>();
        var i = 0;
        while (i < arguments.Count)
        {
            var arg = arguments[i];
            if (arg == "-vf" && i + 1 < arguments.Count)
            {
                displayArgs.Add(arg);
                displayArgs.Add(Quote(arguments[i + 1]));
                i += 2;
            }
            else
            {
                displayArgs.Add(Quote(arg));
                i++;
            }
        }

        return Quote(executablePath) + (displayArgs.Count > 0 ? " " + string.Join(" ", displayArgs) : "");
    }
}


