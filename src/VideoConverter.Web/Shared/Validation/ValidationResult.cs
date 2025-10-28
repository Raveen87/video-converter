namespace VideoConverter.Web.Shared.Validation;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;

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
