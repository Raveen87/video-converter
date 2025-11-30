using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class DirectoryBatchService(
    AppDbContext db,
    IFileSystemValidator fileSystemValidator,
    ILogger<DirectoryBatchService> logger) : IDirectoryBatchService
{
    public async Task<int> CreateBatchAsync(DirectoryQueueRequestDto request, int? profileId, CancellationToken ct)
    {
        logger.LogInformation("Creating directory queue batch for {Directory} -> {Output}", request.DirectoryPath, request.OutputDirectory);

        var extensions = NormalizeExtensions(request.Extensions);

        _ = fileSystemValidator.IsValidDirectory(request.DirectoryPath, out var directoryError);
        if (directoryError is not null)
        {
            logger.LogWarning("Directory validation failed: {Error}", directoryError);

            // Create a failed batch immediately to report error
            var failed = new DirectoryQueueBatch
            {
                CreatedAtUtc = DateTime.UtcNow,
                Status = "Failed",
                TotalItems = 0,
                ProcessedItems = 0,
                ErrorMessage = directoryError,
                ProfileId = profileId,
                RequestJson = SerializeRequest(request),
            };
            _ = db.DirectoryQueueBatches.Add(failed);
            _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return failed.Id;
        }

        var allFiles = EnumerateCandidateFiles(request.DirectoryPath, extensions);
        logger.LogInformation("Found {Count} candidate files for batch", allFiles.Count);

        var batch = new DirectoryQueueBatch
        {
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Pending",
            TotalItems = allFiles.Count,
            ProcessedItems = 0,
            ProfileId = profileId,
            RequestJson = SerializeRequest(request),
        };

        if (allFiles.Count > 0)
        {
            foreach (var path in allFiles)
            {
                batch.Items.Add(new DirectoryQueueBatchItem
                {
                    InputPath = path,
                    Status = "Pending",
                });
            }
        }

        _ = db.DirectoryQueueBatches.Add(batch);
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return batch.Id;
    }

    private static string SerializeRequest(DirectoryQueueRequestDto request)
    {
        return JsonSerializer.Serialize(request);
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
}
