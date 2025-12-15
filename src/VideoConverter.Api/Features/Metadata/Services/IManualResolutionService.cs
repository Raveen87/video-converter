namespace VideoConverter.Api.Features.Metadata.Services;

public interface IManualResolutionService
{
    Task ResolveMetadataQueueItemAsync(int queueId, ResolveMetadataRequestDto request, CancellationToken cancellationToken);

    Task<BatchResolutionResultDto> ResolveBatchMetadataQueueItemsAsync(ResolveBatchMetadataQueueRequestDto request, CancellationToken cancellationToken);

    Task ResolveStreamSelectionQueueItemAsync(int queueId, ResolveStreamSelectionRequestDto request, CancellationToken cancellationToken);

    Task DeleteQueueItemAsync(int queueId, CancellationToken cancellationToken);
}
