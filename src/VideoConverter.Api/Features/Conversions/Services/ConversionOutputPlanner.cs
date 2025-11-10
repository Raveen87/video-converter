using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class ConversionOutputPlanner : IConversionOutputPlanner
{
    public (string OutputFile, string VideoTitle) Plan(Conversion conversion)
    {
        if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
        {
            throw new InvalidOperationException("InputPath and OutputDirectory are required.");
        }

        var desiredBaseName = !string.IsNullOrWhiteSpace(conversion.MetadataBasedFilename)
            ? Path.GetFileNameWithoutExtension(conversion.MetadataBasedFilename)
            : Path.GetFileNameWithoutExtension(conversion.InputPath);

        var outputFile = Path.Combine(conversion.OutputDirectory, desiredBaseName + ".mkv");
        var videoTitle = Path.GetFileNameWithoutExtension(conversion.InputPath);
        return (outputFile, videoTitle);
    }
}
