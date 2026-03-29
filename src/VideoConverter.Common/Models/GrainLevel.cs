namespace VideoConverter.Common.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GrainLevel
{
    None,
    Low,
    High,
}
