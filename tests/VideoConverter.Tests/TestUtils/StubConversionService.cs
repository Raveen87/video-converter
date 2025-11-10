using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Domain.Entities;

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
}
