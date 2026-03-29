namespace VideoConverter.Api.Shared.Domain.Entities;

public class Conversion
{
    public int Id { get; set; }

    public string InputPath { get; set; } = string.Empty;

    public string OutputDirectory { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? Rank { get; set; }

    public ConversionStatus Status { get; set; }

    public int? Crf { get; set; }

    public int? AudioBitrateKbps { get; set; }

    public AudioChannels? AudioChannels { get; set; }

    public int? Resolution { get; set; }

    public string? SelectedAudioStreamIndicesCsv { get; set; }

    public string? SelectedSubtitleStreamIndicesCsv { get; set; }

    public string? SelectedExternalSubtitlePathsCsv { get; set; }

    public string? DetectedCrop { get; set; } // format: "w:h:x:y"

    public GrainLevel DetectedGrainLevel { get; set; } = GrainLevel.Low;

    public string? MetadataBasedFilename { get; set; } // filename based on metadata lookup

    public int? TmdbId { get; set; }

    public string? MatchedTitle { get; set; }

    public int? MatchedYear { get; set; }

    public int? MatchedSeason { get; set; }

    public int? MatchedEpisode { get; set; }

    public string? OutputPathTemplate { get; set; }

    public bool VmafEnabled { get; set; }

    public double? VmafScore { get; set; }

    public double? RequiredVmaf { get; set; }
}
