using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VideoConverter.Api.Shared.Infrastructure.Persistence;

public static class Converters
{
    public static (ValueConverter<IEnumerable<string>?, string?> Converter, ValueComparer<IEnumerable<string>?> Comparer) CreateStringListJson()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var listToJson = new ValueConverter<IEnumerable<string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v) ? null : JsonSerializer.Deserialize<IEnumerable<string>>(v!, jsonOptions)!);

        var listComparer = new ValueComparer<IEnumerable<string>?>(
            (l, r) => (l ?? new List<string>()).SequenceEqual(r ?? new List<string>()),
            v => v == null ? 0 : v.Aggregate(0, (a, s) => HashCode.Combine(a, s == null ? 0 : StringComparer.Ordinal.GetHashCode(s))),
            v => v == null ? null : v.ToList());

        return (listToJson, listComparer);
    }

    public static ValueConverter<ConversionStatus, string> CreateConversionStatusString()
    {
        return new ValueConverter<ConversionStatus, string>(
            v => v.ToString(),
            v => Enum.Parse<ConversionStatus>(v));
    }

    public static ValueConverter<AudioChannels?, string?> CreateAudioChannelsString()
    {
        return new ValueConverter<AudioChannels?, string?>(
            v => v == null ? null : v.ToString(),
            v => string.IsNullOrWhiteSpace(v) ? null : Enum.Parse<AudioChannels>(v));
    }

    public static ValueConverter<GrainLevel, string> CreateGrainLevelString()
    {
        return new ValueConverter<GrainLevel, string>(
            v => v.ToString(),
            v => Enum.Parse<GrainLevel>(v));
    }

    public static ValueConverter<NotificationType, string> CreateNotificationTypeString()
    {
        return new ValueConverter<NotificationType, string>(
            v => v.ToString(),
            v => Enum.Parse<NotificationType>(v));
    }

    public static ValueConverter<ManualResolutionType, string> CreateManualResolutionTypeString()
    {
        return new ValueConverter<ManualResolutionType, string>(
            v => v.ToString(),
            v => Enum.Parse<ManualResolutionType>(v));
    }
}
