namespace VideoConverter.Api.Features.Metadata.Services;

public interface ISourceDetector
{
    string? DetectSource(string filenameWithoutExtension);
}
