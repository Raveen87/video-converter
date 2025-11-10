using System.Diagnostics.CodeAnalysis;

namespace VideoConverter.Api.Shared.Domain.ValueObjects;

public static class RankKey
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const char MinChar = '0';
    private const char MaxChar = 'Z';

    public static string Min()
    {
        return MinChar.ToString();
    }

    public static string Max()
    {
        return MaxChar.ToString();
    }

    public static string NextAfter(string key)
    {
        return Between(key, Max());
    }

    public static string NextBefore(string key)
    {
        return Between(Min(), key);
    }

    public static string Between(string a, string b)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(a);
        ArgumentException.ThrowIfNullOrWhiteSpace(b);

        var left = a.ToUpperInvariant();
        var right = b.ToUpperInvariant();

        if (string.CompareOrdinal(left, right) >= 0)
        {
            throw new ArgumentException("Left key must be strictly less than right key.", nameof(a));
        }

        var result = new System.Text.StringBuilder(Math.Max(left.Length, right.Length) + 2);

        var i = 0;
        while (true)
        {
            var ca = i < left.Length ? left[i] : MinChar;
            var cb = i < right.Length ? right[i] : MaxChar;

            if (ca == cb)
            {
                _ = result.Append(ca);
                i++;
                continue;
            }

            var ia = IndexOf(ca);
            var ib = IndexOf(cb);

            if (ib - ia > 1)
            {
                var mid = ia + ((ib - ia) / 2);
                _ = result.Append(Alphabet[mid]);
                return result.ToString();
            }

            _ = result.Append(ca);
            i++;
        }
    }

    [SuppressMessage("Meziantou.Analyzer", "MA0015: Use 'TryGetXXX' pattern", Justification = "Alphabet is fixed and validated")]
    private static int IndexOf(char c)
    {
        var idx = Alphabet.IndexOf(c, StringComparison.Ordinal);
        return idx >= 0 ? idx : throw new ArgumentOutOfRangeException(nameof(c), $"Invalid rank character: {c}");
    }
}
