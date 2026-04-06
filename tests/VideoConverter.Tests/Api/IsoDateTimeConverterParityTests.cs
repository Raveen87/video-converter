using System.Globalization;
using System.Text.Json;

using FluentAssertions;

using VideoConverter.Common.Serialization;

using Xunit;

namespace VideoConverter.Tests.Api;

public sealed class IsoDateTimeConverterParityTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new IsoDateTimeConverter() },
    };

    [Fact]
    public void Serialization_UsesExactExpectedFormat()
    {
        var cases = new[]
        {
            (new DateTime(2024, 12, 31, 23, 59, 0, 123, DateTimeKind.Utc), "2024-12-31 23:59:00.123"),
            (new DateTime(2024, 6, 1, 7, 8, 9, 45, DateTimeKind.Local), "2024-06-01 07:08:09.045"),
            (new DateTime(2024, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified), "2024-01-02 03:04:05.006"),
        };

        foreach (var (input, expected) in cases)
        {
            var json = JsonSerializer.Serialize(input, Options);
            _ = json.Should().Be($"\"{expected}\"");
        }
    }

    [Fact]
    public void Deserialization_AcceptsSupportedInputs()
    {
        var exactFormat = "\"2024-12-31 23:59:00.123\"";
        var exactResult = JsonSerializer.Deserialize<DateTime>(exactFormat, Options);
        _ = exactResult.Should().Be(new DateTime(2024, 12, 31, 23, 59, 0, 123, DateTimeKind.Unspecified));

        var fallbackCases = new[]
        {
            "\"2024-12-31T23:59:00Z\"",
            "\"2024-12-31T23:59:00+02:00\"",
        };

        foreach (var json in fallbackCases)
        {
            var raw = json.Trim('"');
            var expected = DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var result = JsonSerializer.Deserialize<DateTime>(json, Options);
            _ = result.Should().Be(expected);
        }
    }

    [Fact]
    public void Deserialization_MalformedInput_ThrowsJsonException()
    {
        var malformed = new[] { "\"\"", "\"    \"", "\"not-a-date\"", "null", "123" };

        foreach (var json in malformed)
        {
            var act = () => JsonSerializer.Deserialize<DateTime>(json, Options);
            _ = act.Should().Throw<JsonException>();
        }
    }
}
