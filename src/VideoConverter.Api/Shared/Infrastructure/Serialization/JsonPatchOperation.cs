using System.Reflection;
using System.Text.Json.Nodes;

namespace VideoConverter.Api.Shared.Infrastructure.Serialization;

/// <summary>
/// Represents a JSON Patch operation that can be applied to an object.
/// </summary>
/// <remarks>
/// This implementation supports only the "replace" operation and shallow property paths (first segment only).
/// Nested paths like "/address/city" are not supported. Only top-level properties can be modified.
/// </remarks>
public class JsonPatchOperation
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; set; }

    /// <summary>
    /// Applies this patch operation to the specified target object.
    /// </summary>
    /// <param name="target">The object to which the patch operation should be applied.</param>
    /// <exception cref="JsonPatchException">
    /// Thrown when the operation is invalid, the property is not found, the property is not writable,
    /// or the value cannot be applied to the property.
    /// </exception>
    /// <remarks>
    /// Only shallow property paths are supported (e.g., "/PropertyName").
    /// Nested paths (e.g., "/Address/City") are not supported and will only use the first path segment.
    /// </remarks>
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
