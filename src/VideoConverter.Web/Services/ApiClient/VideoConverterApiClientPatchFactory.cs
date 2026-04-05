using System.Text;

using VideoConverter.Common.Models;
using VideoConverter.Web.Models.ApiClient;

namespace VideoConverter.Web.Services.ApiClient;

internal static class VideoConverterApiClientPatchFactory
{
    private static readonly JsonNamingPolicy JsonPatchPathNamingPolicy = JsonNamingPolicy.CamelCase;

    public static (HttpRequestMessage Request, string JsonContent) CreateConversionJsonPatchRequest(int id, ConversionPatchDto patch, Microsoft.Extensions.Logging.ILogger logger)
    {
        var patchOperations = CreateConversionJsonPatchOperations(patch);
        logger.LogInformation("Created {Count} patch operations for conversion {ConversionId}", patchOperations.Count, id);

        var jsonBuilder = new StringBuilder();
        _ = jsonBuilder.Append('[');
        for (var i = 0; i < patchOperations.Count; i++)
        {
            var op = patchOperations[i];
            if (i > 0)
            {
                _ = jsonBuilder.Append(',');
            }

            _ = jsonBuilder.Append('{');
            _ = jsonBuilder.Append("\"op\":\"");
            _ = jsonBuilder.Append(op.Op);
            _ = jsonBuilder.Append("\",\"path\":\"");
            _ = jsonBuilder.Append(op.Path);
            _ = jsonBuilder.Append("\",\"value\":");

            var valueJson = JsonSerializer.Serialize(op.Value);
            _ = jsonBuilder.Append(valueJson);
            _ = jsonBuilder.Append('}');
        }

        _ = jsonBuilder.Append(']');
        var jsonContent = jsonBuilder.ToString();

        var operationPaths = patchOperations.Select(static operation => operation.Path).ToArray();
        logger.LogInformation("Prepared JSON patch for conversion {ConversionId} with {OperationCount} operation(s). Paths: {OperationPaths}", id, patchOperations.Count, operationPaths);
        logger.LogDebug("JSON patch content for conversion {ConversionId}: {JsonContent}", id, jsonContent);
        logger.LogDebug("JSON patch length for conversion {ConversionId}: {Length} bytes", id, jsonContent.Length);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/conversions/{id}")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json-patch+json"),
        };

        return (request, jsonContent);
    }

    public static (HttpRequestMessage Request, string JsonContent) CreateProfileJsonPatchRequest(int id, ProfilePatchRequest patchRequest, Microsoft.Extensions.Logging.ILogger logger)
    {
        var patchOperations = CreateProfileJsonPatchOperations(patchRequest);
        if (patchOperations.Count is 0)
        {
            throw new ArgumentException("No patch operations were generated for the profile update.", nameof(patchRequest));
        }

        var jsonBuilder = new StringBuilder();
        _ = jsonBuilder.Append('[');
        for (var i = 0; i < patchOperations.Count; i++)
        {
            var op = patchOperations[i];
            if (i > 0)
            {
                _ = jsonBuilder.Append(',');
            }

            _ = jsonBuilder.Append('{');
            _ = jsonBuilder.Append("\"op\":\"");
            _ = jsonBuilder.Append(op.Op);
            _ = jsonBuilder.Append("\",\"path\":\"");
            _ = jsonBuilder.Append(op.Path);
            _ = jsonBuilder.Append("\",\"value\":");

            var valueJson = JsonSerializer.Serialize(op.Value);
            _ = jsonBuilder.Append(valueJson);
            _ = jsonBuilder.Append('}');
        }

        _ = jsonBuilder.Append(']');
        var jsonContent = jsonBuilder.ToString();

        var operationPaths = patchOperations.Select(static operation => operation.Path).ToArray();
        logger.LogInformation("Prepared JSON patch for profile {ProfileId} with {OperationCount} operation(s). Paths: {OperationPaths}", id, patchOperations.Count, operationPaths);
        logger.LogDebug("JSON patch content for profile {ProfileId}: {JsonContent}", id, jsonContent);
        logger.LogDebug("JSON patch length for profile {ProfileId}: {Length} bytes", id, jsonContent.Length);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/profiles/{id}")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json-patch+json"),
        };

        return (request, jsonContent);
    }

    private static List<JsonPatchOperation> CreateConversionJsonPatchOperations(ConversionPatchDto patch)
    {
        var patchOperations = new List<JsonPatchOperation>();

        if (patch.OutputDirectory is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.OutputDirectory)), Value = patch.OutputDirectory });
        }

        if (patch.Crf is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.Crf)), Value = patch.Crf });
        }

        if (patch.AudioBitrateKbps is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.AudioBitrateKbps)), Value = patch.AudioBitrateKbps });
        }

        if (patch.AudioChannels is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.AudioChannels)), Value = patch.AudioChannels.ToString() });
        }

        if (patch.Resolution is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.Resolution)), Value = patch.Resolution });
        }

        if (patch.OutputFilenameTemplate is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.OutputFilenameTemplate)), Value = patch.OutputFilenameTemplate });
        }

        if (patch.AudioLanguages is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.AudioLanguages)), Value = patch.AudioLanguages });
        }

        if (patch.SubtitleLanguages is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.SubtitleLanguages)), Value = patch.SubtitleLanguages });
        }

        if (patch.VmafEnabled is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.VmafEnabled)), Value = patch.VmafEnabled });
        }

        if (patch.RequiredVmaf is not null || patch.VmafEnabled is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(nameof(ConversionPatchDto.RequiredVmaf)), Value = patch.RequiredVmaf });
        }

        return patchOperations;
    }

    private static List<JsonPatchOperation> CreateProfileJsonPatchOperations(ProfilePatchRequest patchRequest)
    {
        var patch = patchRequest.Patch;
        var changed = new HashSet<string>(patchRequest.ChangedFields, StringComparer.Ordinal);
        var patchOperations = new List<JsonPatchOperation>();

        void AddOperation(string propertyName, object? value)
        {
            if (!changed.Contains(propertyName))
            {
                return;
            }

            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = ToJsonPatchPath(propertyName), Value = value });
        }

        AddOperation(nameof(ProfilePatchDto.Name), patch.Name);
        AddOperation(nameof(ProfilePatchDto.OutputDirectory), patch.OutputDirectory);
        AddOperation(nameof(ProfilePatchDto.Crf), patch.Crf);
        AddOperation(nameof(ProfilePatchDto.AudioBitrateKbps), patch.AudioBitrateKbps);
        AddOperation(nameof(ProfilePatchDto.AudioChannels), patch.AudioChannels?.ToString());
        AddOperation(nameof(ProfilePatchDto.Resolution), patch.Resolution);
        AddOperation(nameof(ProfilePatchDto.OutputFilenameTemplate), patch.OutputFilenameTemplate);
        AddOperation(nameof(ProfilePatchDto.OutputPathTemplate), patch.OutputPathTemplate);
        AddOperation(nameof(ProfilePatchDto.AudioLanguages), patch.AudioLanguages);
        AddOperation(nameof(ProfilePatchDto.SubtitleLanguages), patch.SubtitleLanguages);
        AddOperation(nameof(ProfilePatchDto.VmafEnabled), patch.VmafEnabled);
        AddOperation(nameof(ProfilePatchDto.RequiredVmaf), patch.RequiredVmaf);

        return patchOperations;
    }

    private static string ToJsonPatchPath(string propertyName)
    {
        return $"/{JsonPatchPathNamingPolicy.ConvertName(propertyName)}";
    }

    private sealed class JsonPatchOperation
    {
        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}
