using FluentAssertions;

using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Domain;

public class RankKeyTests
{
    [Fact]
    public void Between_ShouldReturnValueBetween_MinAndMax()
    {
        var key = RankKey.Between(RankKey.Min(), RankKey.Max());
        _ = string.CompareOrdinal(RankKey.Min(), key).Should().BeLessThan(0);
        _ = string.CompareOrdinal(key, RankKey.Max()).Should().BeLessThan(0);
    }

    [Fact]
    public void NextAfter_ShouldIncrease()
    {
        var a = "0";
        var b = RankKey.NextAfter(a);
        _ = string.CompareOrdinal(a, b).Should().BeLessThan(0);
    }

    [Fact]
    public void NextBefore_ShouldDecrease()
    {
        var z = "Z";
        var b = RankKey.NextBefore(z);
        _ = string.CompareOrdinal(b, z).Should().BeLessThan(0);
    }

    [Fact]
    public void Between_ShouldWorkForDenseNeighbors()
    {
        var a = "A0";
        var b = "A1";
        var m = RankKey.Between(a, b);
        _ = string.CompareOrdinal(a, m).Should().BeLessThan(0);
        _ = string.CompareOrdinal(m, b).Should().BeLessThan(0);
    }
}
