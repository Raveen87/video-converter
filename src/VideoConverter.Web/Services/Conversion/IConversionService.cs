using VideoConverter.Web.Domain.Entities;
using VideoConverter.Web.Dtos;

namespace VideoConverter.Web.Services.Conversion;

public interface IConversionService
{
    Task<Domain.Entities.Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken);
}
