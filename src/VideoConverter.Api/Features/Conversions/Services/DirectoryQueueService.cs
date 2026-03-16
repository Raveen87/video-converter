using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class DirectoryQueueService(
    AppDbContext db,
    IConversionService conversionService,
    IRequestValidator<ConversionCreateDto> conversionValidator,
    IRequestValidator<DirectoryQueueRequestDto> directoryRequestValidator,
    INotificationService notificationService,
    ILogger<DirectoryQueueService> logger) : IDirectoryQueueService
{
    public async Task<DirectoryQueueResultDto> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken ct)
    {
        logger.LogInformation("Queueing directory: {Directory} to output {Output}", request.DirectoryPath, request.OutputDirectory);

        var validation = await directoryRequestValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            var reason = BuildValidationReason(validation);
            logger.LogWarning("Directory validation failed: {Error}", reason);
            var outcome = new FileQueueOutcome(InputPath: request.DirectoryPath, Status: "invalid", Reason: reason, ConversionId: null);
            return new DirectoryQueueResultDto(0, 0, 0, 1, new[] { outcome });
        }

        var extensions = NormalizeExtensions(request.Extensions);
        var allFiles = EnumerateCandidateFiles(request.DirectoryPath, extensions);
        logger.LogInformation("Found {Count} candidate files", allFiles.Count);

        if (allFiles.Count is 0)
        {
            return new DirectoryQueueResultDto(0, 0, 0, 0, Array.Empty<FileQueueOutcome>());
        }

        var effectiveOutputDirectory = ComputeEffectiveOutputDirectory(profileOutputDirectory: null, requestOutputDirectory: request.OutputDirectory);
        var existingSet = await GetExistingInputSetAsync(outputDirectory: effectiveOutputDirectory, ct: ct).ConfigureAwait(false);

        return await ProcessAndQueueAsync(files: allFiles, request: request, existingSet: existingSet, ct: ct).ConfigureAwait(false);
    }

    private static string[] NormalizeExtensions(IEnumerable<string>? rawExtensions)
    {
        return (rawExtensions is null || !rawExtensions.Any())
            ? [
                ".mkv",
                ".mp4",
            ]
            : [
                .. rawExtensions
                    .Where(static e => !string.IsNullOrWhiteSpace(e))
                    .Select(static e => e.StartsWith('.') ? e : $".{e}")
                    .Distinct(comparer: StringComparer.OrdinalIgnoreCase),
            ];
    }

    private static List<string> EnumerateCandidateFiles(string directoryPath, IReadOnlyCollection<string> extensions)
    {
        return [
            .. Directory
                .EnumerateFiles(directoryPath, searchPattern: "*", searchOption: SearchOption.AllDirectories)
                .Where(p => extensions.Contains(Path.GetExtension(p), comparer: StringComparer.OrdinalIgnoreCase))
                .Order(comparer: StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static string ComputeEffectiveOutputDirectory(string? profileOutputDirectory, string requestOutputDirectory)
    {
        return !string.IsNullOrWhiteSpace(profileOutputDirectory) ? profileOutputDirectory! : requestOutputDirectory;
    }

    private static string BuildValidationReason(DomainValidationResult validation)
    {
        return string.Join(
            "; ",
            validation.ToDictionary().SelectMany(static kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}")));
    }

    private static ConversionCreateDto BuildEffectiveRequest(DirectoryQueueRequestDto request, string inputPath)
    {
        var perFileRequest = new ConversionCreateDto(
            inputPath,
            request.OutputDirectory,
            request.Crf,
            request.AudioBitrateKbps,
            request.AudioChannels,
            request.Resolution,
            request.AudioLanguages,
            request.SubtitleLanguages,
            request.OutputFilenameTemplate,
            request.OutputPathTemplate,
            request.VmafEnabled,
            request.RequiredVmaf);

        return perFileRequest;
    }

    private async Task<DirectoryQueueResultDto> ProcessAndQueueAsync(
        IReadOnlyList<string> files,
        DirectoryQueueRequestDto request,
        HashSet<string> existingSet,
        CancellationToken ct)
    {
        List<FileQueueOutcome> outcomes = [];
        List<Shared.Domain.Entities.Conversion> conversionsToAdd = [];
        var localDuplicateSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var total = files.Count;
        var queued = 0;
        var skipped = 0;
        var invalid = 0;

        foreach (var inputPath in files)
        {
            ct.ThrowIfCancellationRequested();

            if (existingSet.Contains(inputPath) || !localDuplicateSet.Add(inputPath))
            {
                skipped++;
                outcomes.Add(new FileQueueOutcome(InputPath: inputPath, Status: "skipped", Reason: "duplicate", ConversionId: null));
                continue;
            }

            var effectiveRequest = BuildEffectiveRequest(request, inputPath);
            var validation = await conversionValidator.ValidateAsync(effectiveRequest, cancellationToken: ct).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                invalid++;
                var reason = BuildValidationReason(validation);
                outcomes.Add(new FileQueueOutcome(InputPath: inputPath, Status: "invalid", Reason: reason, ConversionId: null));
                continue;
            }

            var conversion = await conversionService.CreateConversionAsync(effectiveRequest, cancellationToken: ct).ConfigureAwait(false);
            conversionsToAdd.Add(conversion);
        }

        if (conversionsToAdd.Count is 0)
        {
            var orderedOutcomesNoSave = outcomes.OrderBy(static o => o.InputPath, comparer: StringComparer.OrdinalIgnoreCase).ToList();
            return new DirectoryQueueResultDto(total, queued, skipped, invalid, orderedOutcomesNoSave);
        }

        db.Conversions.AddRange(conversionsToAdd);
        _ = await db.SaveChangesAsync(cancellationToken: ct).ConfigureAwait(false);
        foreach (var c in conversionsToAdd)
        {
            queued++;
            outcomes.Add(new FileQueueOutcome(InputPath: c.InputPath ?? string.Empty, Status: "queued", Reason: null, ConversionId: c.Id));

            await AddNotificationForConversionAddedAsync(c, ct).ConfigureAwait(false);
        }

        var orderedOutcomes = outcomes.OrderBy(static o => o.InputPath, comparer: StringComparer.OrdinalIgnoreCase).ToList();
        return new DirectoryQueueResultDto(total, queued, skipped, invalid, orderedOutcomes);
    }

    private async Task AddNotificationForConversionAddedAsync(Shared.Domain.Entities.Conversion conversion, CancellationToken ct)
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

    private async Task<HashSet<string>> GetExistingInputSetAsync(string outputDirectory, CancellationToken ct)
    {
        var inputs = await db.Conversions
            .AsNoTracking()
            .Where(c => c.OutputDirectory == outputDirectory)
            .Select(c => c.InputPath)
            .ToListAsync(cancellationToken: ct)
            .ConfigureAwait(false);
        return new HashSet<string>(inputs.Where(path => path is not null), StringComparer.OrdinalIgnoreCase);
    }
}
