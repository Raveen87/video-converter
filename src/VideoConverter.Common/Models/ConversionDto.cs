namespace VideoConverter.Common.Models;

public record ConversionDto(
    int Id,
    string InputPath,
    string OutputDirectory,
    DateTime CreatedAtUtc,
    ConversionStatus Status,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename,
    DateTime? DeletedAtUtc,
    int? TmdbId,
    string? MatchedTitle,
    int? MatchedYear,
    int? MatchedSeason,
    int? MatchedEpisode,
    string? SelectedAudioStreamIndicesCsv,
    string? SelectedSubtitleStreamIndicesCsv,
    string? SelectedExternalSubtitlePathsCsv,
    string? DetectedCrop,
    string? OutputPathTemplate = null);
