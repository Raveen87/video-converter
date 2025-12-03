namespace VideoConverter.Api.Features.Metadata.Services;

public interface IMetadataResolutionService
{
    Task ResolveQueueItemAsync(int queueId, ResolveMetadataRequestDto request, CancellationToken cancellationToken);

    Task ResolveBatchQueueItemsAsync(ResolveBatchMetadataQueueRequestDto request, CancellationToken cancellationToken);

    Task DeleteQueueItemAsync(int queueId, CancellationToken cancellationToken);
}
