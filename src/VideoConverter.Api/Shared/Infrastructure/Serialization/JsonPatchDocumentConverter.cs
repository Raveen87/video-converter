namespace VideoConverter.Api.Shared.Infrastructure.Serialization;

public class JsonPatchDocumentConverter<T> : JsonConverter<JsonPatchDocument<T>>
    where T : class
{
    public override JsonPatchDocument<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            throw new JsonException("Expected JSON Array for Patch Document.");
        }

        var doc = new JsonPatchDocument<T>();

        var operations = JsonSerializer.Deserialize<List<JsonPatchOperation>>(ref reader, options);

        if (operations is not null)
        {
            foreach (var op in operations)
            {
                doc.Operations.Add(op);
            }
        }

        return doc;
    }

    public override void Write(Utf8JsonWriter writer, JsonPatchDocument<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Operations, options);
    }
}
