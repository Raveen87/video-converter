using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Common.Models;

namespace VideoConverter.Tests.TestUtils;

internal sealed class StubConversionService : IConversionService
{
    public Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        var c = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Crf = request.Crf,
            AudioBitrateKbps = request.AudioBitrateKbps,
            Resolution = request.Resolution,
        };
        return Task.FromResult(c);
    }

    public Task<Conversion> CreateConversionWithMetadataAsync(ConversionCreateDto request, MediaMetadata metadata, CancellationToken cancellationToken)
    {
        // For testing, behave same as normal create
        return CreateConversionAsync(request, cancellationToken);
    }

    public Task<LanguageProcessingResult> ProcessLanguagesAsync(string inputPath, IEnumerable<string>? audioLanguages, IEnumerable<string>? subtitleLanguages, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Not used in this test");
    }
}
