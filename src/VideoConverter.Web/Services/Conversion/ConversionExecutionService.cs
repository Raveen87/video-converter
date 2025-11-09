using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Shared.Domain.ValueObjects;
using VideoConverter.Web.Shared.Infrastructure.Persistence;

namespace VideoConverter.Web.Services.Conversion;

public sealed class ConversionExecutionService(AppDbContext db, IConversionRunner runner, ILogger<ConversionExecutionService> logger)
    : IConversionExecutionService
{
    public async Task<ConversionStartResult> RunByIdAsync(int id, CancellationToken ct)
    {
        var conversion = await db.Conversions.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (conversion is null)
        {
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = $"Conversion with ID {id} was not found." };
        }

        conversion.Status = ConversionStatus.Running;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        ConversionStartResult result;
        try
        {
            result = await runner.StartAsync(conversion, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Runner threw for conversion {ConversionId}", id);
            result = new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        conversion.Status = result.IsSuccess ? ConversionStatus.Completed : ConversionStatus.Failed;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return result;
    }

    public async Task<(ConversionStartResult Result, int ConversionId)> RunNextPendingAsync(CancellationToken ct)
    {
        var conversion = await db.Conversions
            .OrderBy(c => c.Rank)
            .ThenBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Pending, ct)
            .ConfigureAwait(false);

        if (conversion is null)
        {
            return (new ConversionStartResult { IsSuccess = false, ErrorMessage = "No conversions found in the queue." }, 0);
        }

        conversion.Status = ConversionStatus.Running;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        ConversionStartResult result;
        try
        {
            result = await runner.StartAsync(conversion, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Runner threw for conversion {ConversionId}", conversion.Id);
            result = new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        conversion.Status = result.IsSuccess ? ConversionStatus.Completed : ConversionStatus.Failed;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return (result, conversion.Id);
    }
}
