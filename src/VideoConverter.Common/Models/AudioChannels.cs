namespace VideoConverter.Common.Models;

/// <summary>
/// Audio channel configuration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AudioChannels>))]
public enum AudioChannels
{
    Stereo20,
    Surround51,
}
