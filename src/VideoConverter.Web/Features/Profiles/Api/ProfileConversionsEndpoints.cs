using VideoConverter.Web.Common.Exceptions;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Profiles.Api;

public sealed class ProfileConversionsEndpoints(
    AppDbContext db,
    ProfileExistsValidator profileExistsValidator,
    EffectiveConversionRequestBuilder effectiveRequestBuilder,
    IRequestValidator<ConversionCreateDto> conversionValidator,
    IConversionService conversionService,
    IDirectoryQueueService directoryQueueService,
    ILogger<ProfileConversionsEndpoints> logger)
{
    public async Task<IResult> QueueConversionAsync(int id, ProfileConversionCreateDto request, CancellationToken ct)
    {
        logger.LogDebug("Queueing single conversion for profile {ProfileId}: {Input} -> {Output}", id, request.InputPath, request.OutputDirectory);

        var profile = await profileExistsValidator.GetProfileForReadAsync(id, db, ct).ConfigureAwait(false);

        var baseRequest = new ConversionCreateDto(
            request.InputPath,
            request.OutputDirectory,
            profile.Crf,
            profile.AudioBitrateKbps,
            profile.AudioChannels,
            profile.Resolution,
            profile.AudioLanguages,
            profile.SubtitleLanguages,
            profile.OutputFilenameTemplate);

        // Ensure request-specified output directory takes precedence
        var effective = effectiveRequestBuilder.Build(baseRequest, profile) with { OutputDirectory = request.OutputDirectory };

        var validation = await conversionValidator.ValidateAsync(effective, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            logger.LogWarning("Validation failed for profile conversion request {ProfileId}", id);
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var conversion = await conversionService.CreateConversionAsync(effective, ct).ConfigureAwait(false);
            _ = db.Conversions.Add(conversion);
            _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Results.Created($"/api/conversions/{conversion.Id}", ToDto(conversion));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Conversion creation failed: {Error}", ex.Message);
            throw ApiErrorException.ValidationError(
                ex.Message,
                "The conversion request is invalid. Please check your input parameters and try again.",
                ex);
        }
    }

    public async Task<IResult> QueueDirectoryAsync(int id, ProfileDirectoryQueueRequestDto request, CancellationToken ct)
    {
        logger.LogInformation("Queueing directory for profile {ProfileId}: {Dir} -> {Out}", id, request.DirectoryPath, request.OutputDirectory);

        var profile = await profileExistsValidator.GetProfileForReadAsync(id, db, ct).ConfigureAwait(false);

        var dto = new DirectoryQueueRequestDto(
            DirectoryPath: request.DirectoryPath,
            OutputDirectory: request.OutputDirectory,
            Crf: profile.Crf,
            AudioBitrateKbps: profile.AudioBitrateKbps,
            AudioChannels: profile.AudioChannels,
            Resolution: profile.Resolution,
            AudioLanguages: profile.AudioLanguages,
            SubtitleLanguages: profile.SubtitleLanguages,
            OutputFilenameTemplate: profile.OutputFilenameTemplate,
            Extensions: request.Extensions);

        var result = await directoryQueueService.QueueDirectoryAsync(dto, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static ConversionDto ToDto(Shared.Domain.Entities.Conversion c)
    {
        return new ConversionDto(
            c.Id,
            c.InputPath,
            c.OutputDirectory,
            c.CreatedAtUtc,
            c.Crf,
            c.AudioBitrateKbps,
            c.AudioChannels,
            c.Resolution,
            c.MetadataBasedFilename);
    }
}
