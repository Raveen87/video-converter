using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VideoConverter.Api.Shared.Domain.ValueObjects;

[JsonConverter(typeof(StringEnumConverter))]
public enum AudioChannels
{
    Stereo20 = 20,
    Surround51 = 51,
}
