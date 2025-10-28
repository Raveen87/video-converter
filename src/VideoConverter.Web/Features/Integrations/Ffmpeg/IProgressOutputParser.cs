namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IProgressOutputParser
{
    bool TryParseTimeSeconds(string line, out double seconds);
}

