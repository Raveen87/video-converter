using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Conversions.Api;

internal static class ConversionsApiHelpers
{
    public static ConversionDto ToDto(Conversion c)
    {
        return new ConversionDto(
            Id: c.Id,
            InputPath: c.InputPath,
            OutputDirectory: c.OutputDirectory,
            CreatedAtUtc: c.CreatedAtUtc,
            Status: c.Status,
            Crf: c.Crf,
            AudioBitrateKbps: c.AudioBitrateKbps,
            AudioChannels: c.AudioChannels,
            Resolution: c.Resolution,
            MetadataBasedFilename: c.MetadataBasedFilename,
            DeletedAtUtc: c.DeletedAtUtc,
            TmdbId: c.TmdbId,
            MatchedTitle: c.MatchedTitle,
            MatchedYear: c.MatchedYear,
            MatchedSeason: c.MatchedSeason,
            MatchedEpisode: c.MatchedEpisode,
            SelectedAudioStreamIndicesCsv: c.SelectedAudioStreamIndicesCsv,
            SelectedSubtitleStreamIndicesCsv: c.SelectedSubtitleStreamIndicesCsv,
            SelectedExternalSubtitlePathsCsv: c.SelectedExternalSubtitlePathsCsv,
            DetectedCrop: c.DetectedCrop,
            OutputPathTemplate: c.OutputPathTemplate,
            VmafEnabled: c.VmafEnabled,
            RequiredVmaf: c.RequiredVmaf,
            VmafScore: c.VmafScore,
            DetectedGrainLevel: c.DetectedGrainLevel);
    }

    public static void ThrowConversionStartError(ConversionStartResult result)
    {
        if (result.ErrorDetails?.ContainsKey("runningConversionId") is true)
        {
            throw ApiErrorException.BusinessError(
                ErrorCode.ConversionAlreadyStarted,
                result.ErrorMessage ?? "A conversion is already running.",
                "Another conversion is currently in progress. Please stop or pause the running conversion before starting a new one.",
                innerException: null,
                result.ErrorDetails);
        }

        throw ApiErrorException.BusinessError(
            ErrorCode.ProcessStartFailed,
            result.ErrorMessage ?? "Unknown error occurred",
            "Failed to start the conversion process. Please try again later.",
            innerException: null,
            result.ErrorDetails);
    }

    public static async Task AddNotificationForConversionAddedAsync(Conversion conversion, INotificationService notificationService, CancellationToken ct)
    {
        try
        {
            var title = "Conversion Added";
            var message = $"Conversion {conversion.Id} has been added to the queue. Input: {Path.GetFileName(conversion.InputPath)}";
            _ = await notificationService.AddAsync(title, message, NotificationType.Info, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Silently fail - notification is not critical
        }
    }

    public static async Task AddNotificationForConversionDeletedAsync(Conversion conversion, INotificationService notificationService, CancellationToken ct)
    {
        try
        {
            var title = "Conversion Deleted";
            var message = $"Conversion {conversion.Id} has been deleted from the queue. Input: {Path.GetFileName(conversion.InputPath)}";
            _ = await notificationService.AddAsync(title, message, NotificationType.Info, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Silently fail - notification is not critical
        }
    }
}
