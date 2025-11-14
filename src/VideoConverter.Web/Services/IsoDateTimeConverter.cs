using System.Globalization;

namespace VideoConverter.Web.Services;

/// <summary>
/// JSON converter that formats DateTime values in ISO format:
/// - Timestamp: "yyyy-MM-dd HH:mm:ss.fff" (e.g., "2024-12-31 23:59:00.000").
/// </summary>
public sealed class IsoDateTimeConverter : JsonConverter<DateTime>
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                throw new JsonException("Cannot convert null or empty string to DateTime.");
            }

            // Try parsing the ISO format first
            if (DateTime.TryParseExact(stringValue, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            // Fall back to standard ISO 8601 parsing for backward compatibility
            if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            throw new JsonException($"Unable to parse DateTime from string: {stringValue}");
        }

        if (reader.TokenType is JsonTokenType.Null)
        {
            throw new JsonException("Cannot convert null to DateTime.");
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var formatted = value.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        writer.WriteStringValue(formatted);
    }
}
