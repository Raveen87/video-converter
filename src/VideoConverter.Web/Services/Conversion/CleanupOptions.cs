namespace VideoConverter.Web.Services.Conversion;

public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    public bool Enabled { get; init; } = true;

    public int RetentionDays { get; init; } = 30;
}
