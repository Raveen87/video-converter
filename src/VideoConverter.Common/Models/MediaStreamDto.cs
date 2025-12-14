namespace VideoConverter.Common.Models;

/// <summary>
/// Represents an audio or subtitle stream from a media file.
/// </summary>
public class MediaStreamDto
{
    public int Index { get; set; }

    public string CodecType { get; set; } = string.Empty;

    public string? CodecName { get; set; }

    public string? LanguageTag { get; set; }

    public string? Title { get; set; }

    public int? Channels { get; set; }

    public string? ChannelLayout { get; set; }

    public bool IsDefault { get; set; }

    public bool IsForced { get; set; }
}
