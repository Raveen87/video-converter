using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace VideoConverter.Tests.TestUtils.ContractSnapshots;

internal static class ContractSnapshotAssert
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly HashSet<string> VolatileKeys = new(StringComparer.Ordinal)
    {
        "timestamp",
        "traceId",
        "requestId",
    };

    public static void PayloadMatchesSnapshot(string snapshotRelativePath, object? payload)
    {
        var snapshotPath = ResolveSnapshotPath(snapshotRelativePath);
        _ = File.Exists(snapshotPath)
            ? true
            : throw new Xunit.Sdk.XunitException($"Snapshot file not found: {snapshotPath}");

        var expected = File.ReadAllText(snapshotPath, Encoding.UTF8);
        var actual = NormalizePayload(payload);

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new Xunit.Sdk.XunitException(
                $"Contract snapshot mismatch: {snapshotRelativePath}{Environment.NewLine}" +
                "--- Expected ---" + Environment.NewLine +
                expected + Environment.NewLine +
                "--- Actual ---" + Environment.NewLine +
                actual);
        }
    }

    private static string NormalizePayload(object? payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var node = JsonNode.Parse(json);
        var normalizedNode = Canonicalize(node);

        return JsonSerializer.Serialize(normalizedNode, SerializerOptions) + Environment.NewLine;
    }

    private static JsonNode? Canonicalize(JsonNode? node)
    {
        return node switch
        {
            null => null,
            JsonObject obj => CanonicalizeObject(obj),
            JsonArray arr => CanonicalizeArray(arr),
            _ => node.DeepClone(),
        };
    }

    private static JsonNode CanonicalizeObject(JsonObject source)
    {
        var canonical = new JsonObject();
        foreach (var property in source.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (VolatileKeys.Contains(property.Key))
            {
                continue;
            }

            canonical[property.Key] = Canonicalize(property.Value);
        }

        return canonical;
    }

    private static JsonNode CanonicalizeArray(JsonArray source)
    {
        var canonical = new JsonArray();
        foreach (var item in source)
        {
            canonical.Add(Canonicalize(item));
        }

        return canonical;
    }

    private static string ResolveSnapshotPath(string snapshotRelativePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(projectRoot, "tests", "VideoConverter.Tests", "Api", "ContractSnapshots", snapshotRelativePath);
    }
}
