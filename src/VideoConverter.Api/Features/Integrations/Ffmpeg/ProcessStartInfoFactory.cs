namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class ProcessStartInfoFactory
{
    public static ProcessStartInfo Build(string executablePath, IReadOnlyList<string> arguments, string? workingDirectory, IReadOnlyDictionary<string, string>? environment, bool redirectStandardInput)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = redirectStandardInput,
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (environment is not null)
        {
            foreach (var kvp in environment)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        return startInfo;
    }
}
