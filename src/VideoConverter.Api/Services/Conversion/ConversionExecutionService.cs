using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;

namespace VideoConverter.Api.Services.Conversion;

public sealed class ConversionExecutionService(AppDbContext db, IConversionRunner runner, INotificationService notificationService, ILogger<ConversionExecutionService> logger)
    : IConversionExecutionService
{
    public async Task<ConversionStartResult> RunByIdAsync(int id, CancellationToken ct)
    {
        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running && c.Id != id, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            logger.LogWarning(
                "Cannot start conversion {ConversionId}: conversion {RunningConversionId} is already running",
                id,
                runningConversion.Id);
            var errorDetails = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "runningConversionId", runningConversion.Id },
            };
            return new ConversionStartResult
            {
                IsSuccess = false,
                ErrorMessage = $"A conversion is already running. Please pause or stop conversion {runningConversion.Id} before starting a new one.",
                ErrorDetails = errorDetails,
            };
        }

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

        await AddNotificationForConversionAsync(conversion, result, CancellationToken.None).ConfigureAwait(false);

        return result;
    }

    public async Task<(ConversionStartResult Result, int ConversionId)> RunNextPendingAsync(CancellationToken ct)
    {
        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            logger.LogWarning(
                "Cannot start next conversion: conversion {RunningConversionId} is already running",
                runningConversion.Id);
            var errorDetails = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "runningConversionId", runningConversion.Id },
            };
            return (new ConversionStartResult
            {
                IsSuccess = false,
                ErrorMessage = $"A conversion is already running. Please pause or stop conversion {runningConversion.Id} before starting a new one.",
                ErrorDetails = errorDetails,
            }, 0);
        }

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

        await AddNotificationForConversionAsync(conversion, result, CancellationToken.None).ConfigureAwait(false);

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

    private async Task AddNotificationForConversionAsync(Shared.Domain.Entities.Conversion conversion, ConversionStartResult result, CancellationToken ct)
    {
        try
        {
            if (result.IsSuccess)
            {
                var title = "Conversion Completed";
                var message = $"Conversion {conversion.Id} completed successfully. Input: {Path.GetFileName(conversion.InputPath)}";
                _ = await notificationService.AddAsync(title, message, NotificationType.Success, ct).ConfigureAwait(false);
            }
            else
            {
                var title = "Conversion Failed";
                var message = $"Conversion {conversion.Id} failed. Input: {Path.GetFileName(conversion.InputPath)}. Error: {result.ErrorMessage ?? "Unknown error"}";
                _ = await notificationService.AddAsync(title, message, NotificationType.Error, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to add notification for conversion {ConversionId}", conversion.Id);
        }
    }
}
