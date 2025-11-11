using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoConverter.Web.Models;

public sealed class AudioChannelsConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType is JsonTokenType.String)
        {
            return reader.GetString();
        }

        if (reader.TokenType is JsonTokenType.Number)
        {
            // Handle numeric enum values (20, 51) and convert to string representation
            var value = reader.GetInt32();
            return value switch
            {
                20 => "Stereo20",
                51 => "Surround51",
                _ => value.ToString(),
            };
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
