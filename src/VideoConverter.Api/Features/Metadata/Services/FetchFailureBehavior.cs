namespace VideoConverter.Api.Features.Metadata.Services;

public enum FetchFailureBehavior
{
    DefaultToInputName,
    QueueForManualResolution,
    SkipItem,
}
