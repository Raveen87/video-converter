using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;

namespace VideoConverter.Web.Services;

public interface IConversionService
{
    Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken);
}
