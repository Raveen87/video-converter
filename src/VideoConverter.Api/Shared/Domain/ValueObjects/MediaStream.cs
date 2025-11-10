namespace VideoConverter.Api.Shared.Domain.ValueObjects;

public sealed class MediaStream
{
    public required int Index { get; init; }

    public required string CodecType { get; init; } // e.g., "audio", "video", "subtitle"

    public string? LanguageTag { get; init; } // e.g., "eng"

    public string? Title { get; init; } // stream title/name

    public string? CodecName { get; init; } // e.g., "subrip", "ass", "webvtt", "mov_text", "hdmv_pgs_subtitle"
}
