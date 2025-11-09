namespace VideoConverter.Web.Features.Metadata.Services;

public enum FetchFailureBehavior
{
    DefaultToInputName,
    QueueForManualResolution,
    SkipItem,
}
