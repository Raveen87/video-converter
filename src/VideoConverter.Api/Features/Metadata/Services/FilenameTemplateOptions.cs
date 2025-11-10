namespace VideoConverter.Api.Features.Metadata.Services;

public sealed class FilenameTemplateOptions
{
    public const string SectionName = "FilenameTemplate";

    public string? DefaultTemplate { get; set; }
}
