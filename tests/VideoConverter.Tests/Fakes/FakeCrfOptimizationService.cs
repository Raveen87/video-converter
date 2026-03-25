using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Tests.Fakes;

public class FakeCrfOptimizationService : ICrfOptimizationService
{
    public Task<int> OptimizeCrfAsync(Conversion conversion, double requiredVmaf, int startCrf, CancellationToken ct)
    {
        return Task.FromResult(startCrf);
    }
}
