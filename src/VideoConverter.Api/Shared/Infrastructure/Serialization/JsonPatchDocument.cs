namespace VideoConverter.Api.Shared.Infrastructure.Serialization;

[JsonConverter(typeof(JsonPatchDocumentConverterFactory))]
public class JsonPatchDocument<T>
    where T : class
{
    public IList<JsonPatchOperation> Operations { get; } = [];

    public void ApplyTo(T objectToApplyTo)
    {
        foreach (var op in Operations)
        {
            op.Apply(objectToApplyTo);
        }
    }
}
