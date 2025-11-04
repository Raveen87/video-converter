using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.Entities;

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
