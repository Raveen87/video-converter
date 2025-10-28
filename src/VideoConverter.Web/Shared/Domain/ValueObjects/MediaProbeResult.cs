namespace VideoConverter.Web.Shared.Domain.ValueObjects;

public sealed class MediaProbeResult
{
    public required IReadOnlyList<MediaStream> Streams { get; init; }

    public int? VideoWidth { get; init; }

    public int? VideoHeight { get; init; }

    public double? DurationSeconds { get; init; }
}
