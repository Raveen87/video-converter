using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IConversionService
{
    Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken);
}
