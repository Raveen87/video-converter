namespace VideoConverter.Api.Features.Conversions.Services;

public record LanguageProcessingResult
{
    public string? AudioIndicesCsv { get; init; }

    public string? SubtitleIndicesCsv { get; init; }

    public string? ExternalSubtitlePathsCsv { get; init; }
}
