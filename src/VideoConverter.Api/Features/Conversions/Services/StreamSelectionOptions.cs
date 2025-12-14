namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class StreamSelectionOptions
{
    public const string SectionName = "StreamSelection";

    /// <summary>
    /// Gets the behavior when required audio/subtitle streams are not found.
    /// </summary>
    public StreamSelectionFailureBehavior OnFailureBehavior { get; init; } = StreamSelectionFailureBehavior.QueueForManualResolution;
}
