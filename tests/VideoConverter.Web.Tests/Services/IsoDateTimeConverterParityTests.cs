using System.Globalization;
using System.Text.Json;

using VideoConverter.Common.Serialization;

using Xunit;

namespace VideoConverter.Web.Tests.Services;

public sealed class IsoDateTimeConverterParityTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new IsoDateTimeConverter() },
    };

    [Theory]
    [InlineData(2024, 12, 31, 23, 59, 0, 123, DateTimeKind.Utc, "2024-12-31 23:59:00.123")]
    [InlineData(2024, 6, 1, 7, 8, 9, 45, DateTimeKind.Local, "2024-06-01 07:08:09.045")]
    [InlineData(2024, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified, "2024-01-02 03:04:05.006")]
    public void Serialization_UsesExactExpectedFormat(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        DateTimeKind kind,
        string expected)
    {
        var input = new DateTime(year, month, day, hour, minute, second, millisecond, kind);

        var json = JsonSerializer.Serialize(input, Options);

        Assert.Equal($"\"{expected}\"", json);
    }

    [Fact]
    public void Deserialization_AcceptsSupportedInputs()
    {
        var exactFormat = "\"2024-12-31 23:59:00.123\"";
        var exactResult = JsonSerializer.Deserialize<DateTime>(exactFormat, Options);
        Assert.Equal(new DateTime(2024, 12, 31, 23, 59, 0, 123, DateTimeKind.Unspecified), exactResult);

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
            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"    \"")]
    [InlineData("\"not-a-date\"")]
    [InlineData("null")]
    [InlineData("123")]
    public void Deserialization_MalformedInput_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, Options));
    }
}
