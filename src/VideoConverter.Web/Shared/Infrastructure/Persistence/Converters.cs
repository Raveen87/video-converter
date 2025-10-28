using System.Text.Json;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VideoConverter.Web.Shared.Infrastructure.Persistence;

public static class Converters
{
    public static (ValueConverter<IEnumerable<string>?, string?> converter, ValueComparer<IEnumerable<string>?> comparer) CreateStringListJson()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var listToJson = new ValueConverter<IEnumerable<string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v) ? null : JsonSerializer.Deserialize<IEnumerable<string>>(v!, jsonOptions)!);

        var listComparer = new ValueComparer<IEnumerable<string>?>(
            (l, r) => (l ?? new List<string>()).SequenceEqual(r ?? new List<string>()),
            v => v == null ? 0 : v.Aggregate(0, (a, s) => HashCode.Combine(a, s == null ? 0 : StringComparer.Ordinal.GetHashCode(s))),
            v => v == null ? null : v.ToList());

        return (listToJson, listComparer);
    }
}
