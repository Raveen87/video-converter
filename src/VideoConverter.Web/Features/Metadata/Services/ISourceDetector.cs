namespace VideoConverter.Web.Features.Metadata.Services;

public interface ISourceDetector
{
    string? DetectSource(string filenameWithoutExtension);
}
