namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public interface IProgressOutputParser
{
    bool TryParseTimeSeconds(string line, out double seconds);
}
