namespace VideoConverter.Common.Validation;

public sealed class DomainValidationResult
{
    public bool IsValid => Errors.Count is 0;

    private Dictionary<string, List<string>> Errors { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string field, string message)
    {
        if (!Errors.TryGetValue(field, out var list))
        {
            Errors[field] = list = [];
        }

        list.Add(message);
    }

    public IDictionary<string, string[]> ToDictionary()
    {
        return Errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }
}
