using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Features.Conversions.Dtos;

namespace VideoConverter.Web.Features.Conversions.Services;

public interface IConversionService
{
    Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken);
}
