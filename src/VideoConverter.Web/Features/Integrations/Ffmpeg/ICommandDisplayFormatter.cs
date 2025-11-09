namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface ICommandDisplayFormatter
{
    string Build(string executablePath, IReadOnlyList<string> arguments);
}
