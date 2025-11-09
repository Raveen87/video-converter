namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal sealed class DefaultCommandDisplayFormatter : ICommandDisplayFormatter
{
    public string Build(string executablePath, IReadOnlyList<string> arguments)
    {
        return CommandDisplayFormatter.Build(executablePath, arguments);
    }
}
