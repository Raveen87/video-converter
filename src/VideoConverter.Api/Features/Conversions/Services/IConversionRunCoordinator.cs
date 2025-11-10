namespace VideoConverter.Api.Features.Conversions.Services;

public interface IConversionRunCoordinator
{
    bool TryStart(string key);

    void Stop(string key);
}
