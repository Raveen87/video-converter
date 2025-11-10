using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IConversionOutputPlanner
{
    (string OutputFile, string VideoTitle) Plan(Conversion conversion);
}
