namespace VideoConverter.Api.Shared.Infrastructure.Serialization;

public class JsonPatchDocumentConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(JsonPatchDocument<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var modelType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(JsonPatchDocumentConverter<>).MakeGenericType(modelType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
