namespace VideoConverter.Web.Services.Models;

public sealed class MediaProbeResult
{
    public required IReadOnlyList<MediaStream> Streams { get; init; }
    public int? VideoWidth { get; init; }
    public int? VideoHeight { get; init; }
}

public sealed class MediaStream
{
    public required int Index { get; init; }
    public required string CodecType { get; init; } // e.g., "audio", "video", "subtitle"
    public string? LanguageTag { get; init; } // e.g., "eng"
    public string? Title { get; init; } // stream title/name
}


