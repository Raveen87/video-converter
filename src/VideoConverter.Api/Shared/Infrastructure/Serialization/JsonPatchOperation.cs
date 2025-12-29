using System.Reflection;
using System.Text.Json.Nodes;

namespace VideoConverter.Api.Shared.Infrastructure.Serialization;

public class JsonPatchOperation
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; set; }

    public void Apply(object target)
    {
        if (string.IsNullOrWhiteSpace(Op))
        {
            throw new JsonPatchException("JSON Patch operation 'op' must be specified. Supported operations: 'replace'.");
        }

        if (!string.Equals(Op, "replace", StringComparison.OrdinalIgnoreCase))
        {
            throw new JsonPatchException($"Unsupported JSON Patch operation '{Op}'. Supported operations: 'replace'.");
        }

        var pathSegments = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length is 0)
        {
            return;
        }

        // Simple property traversal (shallow)
        var propName = pathSegments[0];

        var prop = target.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new JsonPatchException($"Property '{propName}' not found on type '{target.GetType().Name}'.");

        if (!prop.CanWrite)
        {
            throw new JsonPatchException($"Property '{propName}' is not writable.");
        }

        try
        {
            object? value = null;
            if (Value is not null)
            {
                value = Value.Deserialize(prop.PropertyType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            prop.SetValue(target, value);
        }
        catch (Exception ex)
        {
            throw new JsonPatchException($"Failed to apply value to property '{propName}'.", ex);
        }
    }
}
