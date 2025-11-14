namespace VideoConverter.Api.Shared.Domain.ValueObjects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AudioChannels
{
    Stereo20,
    Surround51,
}
