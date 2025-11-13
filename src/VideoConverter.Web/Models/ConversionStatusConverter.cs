using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoConverter.Web.Models;

/// <summary>
/// Custom JSON converter for ConversionStatus that handles both string and numeric enum values.
/// This ensures compatibility with the API whether it returns enums as strings or numbers.
/// </summary>
public sealed class ConversionStatusConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        if (reader.TokenType is JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            return value switch
            {
                0 => "Waiting",
                1 => "Running",
                _ => value.ToString(),
            };
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing ConversionStatus");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
