using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface ICrfOptimizationService
{
    Task<int> OptimizeCrfAsync(Conversion conversion, double requiredVmaf, int startCrf, CancellationToken ct);
}
