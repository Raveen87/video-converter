using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Conversions.Services;

public interface IConversionOutputPlanner
{
    (string OutputFile, string VideoTitle) Plan(Conversion conversion);
}
