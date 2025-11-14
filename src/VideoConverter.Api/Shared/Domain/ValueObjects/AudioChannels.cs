using System.Text.Json.Serialization;

namespace VideoConverter.Api.Shared.Domain.ValueObjects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AudioChannels
{
    Stereo20 = 20,
    Surround51 = 51,
}
