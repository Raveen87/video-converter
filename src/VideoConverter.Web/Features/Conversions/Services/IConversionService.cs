using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Conversions.Services;

public interface IConversionService
{
    Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken);
}
