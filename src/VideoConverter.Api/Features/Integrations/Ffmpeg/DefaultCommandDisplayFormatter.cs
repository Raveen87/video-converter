namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal sealed class DefaultCommandDisplayFormatter : ICommandDisplayFormatter
{
    public string Build(string executablePath, IReadOnlyList<string> arguments)
    {
        static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            var needsQuotes = s.Any(ch => char.IsWhiteSpace(ch) || "&|><^()'\"".Contains(ch, StringComparison.Ordinal));
            return needsQuotes ? "\"" + s.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"" : s;
        }

        var displayArgs = new List<string>();
        var i = 0;
        while (i < arguments.Count)
        {
            var arg = arguments[i];
            if (string.Equals(arg, "-vf", StringComparison.Ordinal) && i + 1 < arguments.Count)
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

        return Quote(executablePath) + (displayArgs.Count > 0 ? " " + string.Join(' ', displayArgs) : string.Empty);
    }
}
