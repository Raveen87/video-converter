using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Services.Conversion;

public sealed class ConversionExecutionService(AppDbContext db, IConversionRunner runner, ILogger<ConversionExecutionService> logger)
    : IConversionExecutionService
{
    public async Task<ConversionStartResult> RunByIdAsync(int id, CancellationToken ct)
    {
        var conversion = await db.Conversions.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (conversion is null)
        {
            logger.LogWarning("Conversion with ID {ConversionId} was not found", id);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = $"Conversion with ID {id} was not found." };
        }

        logger.LogInformation(
            "Executing conversion ID {ConversionId}. Input: {InputPath}, Output: {OutputDirectory}, Status: {Status}",
            conversion.Id,
            conversion.InputPath,
            conversion.OutputDirectory,
            conversion.Status);

        conversion.Status = ConversionStatus.Running;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var result = await ExecuteConversionAsync(conversion, CancellationToken.None).ConfigureAwait(false);
        conversion.Status = result.IsSuccess ? ConversionStatus.Completed : ConversionStatus.Failed;
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
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
            logger.LogInformation("No conversions found in the queue");
            return (new ConversionStartResult { IsSuccess = false, ErrorMessage = "No conversions found in the queue." }, 0);
        }

        logger.LogInformation(
            "Executing next pending conversion ID {ConversionId}. Input: {InputPath}, Output: {OutputDirectory}, Rank: {Rank}",
            conversion.Id,
            conversion.InputPath,
            conversion.OutputDirectory,
            conversion.Rank);

        conversion.Status = ConversionStatus.Running;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var result = await ExecuteConversionAsync(conversion, CancellationToken.None).ConfigureAwait(false);
        conversion.Status = result.IsSuccess ? ConversionStatus.Completed : ConversionStatus.Failed;
        _ = await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return (result, conversion.Id);
    }

    private async Task<ConversionStartResult> ExecuteConversionAsync(Shared.Domain.Entities.Conversion conversion, CancellationToken ct)
    {
        ConversionStartResult result;
        try
        {
            result = await runner.StartAsync(conversion, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Runner threw exception for conversion ID {ConversionId}. Input: {InputPath}, Output: {OutputDirectory}. Exception: {ExceptionMessage}",
                conversion.Id,
                conversion.InputPath,
                conversion.OutputDirectory,
                ex.Message);
            result = new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Conversion ID {ConversionId} completed successfully. Input: {InputPath}, Output: {OutputDirectory}, PID: {ProcessId}",
                conversion.Id,
                conversion.InputPath,
                conversion.OutputDirectory,
                result.ProcessId);
        }
        else
        {
            logger.LogError(
                "Conversion ID {ConversionId} failed. Input: {InputPath}, Output: {OutputDirectory}, Error: {ErrorMessage}, ErrorDetails: {ErrorDetails}",
                conversion.Id,
                conversion.InputPath,
                conversion.OutputDirectory,
                result.ErrorMessage,
                result.ErrorDetails);
        }

        return result;
    }
}
