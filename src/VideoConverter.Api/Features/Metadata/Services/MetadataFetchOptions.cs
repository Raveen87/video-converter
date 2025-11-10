namespace VideoConverter.Api.Features.Metadata.Services;

public sealed class MetadataFetchOptions
{
    public const string SectionName = "MetadataFetch";

    public FetchFailureBehavior OnFailureBehavior { get; init; } = FetchFailureBehavior.QueueForManualResolution;
}
