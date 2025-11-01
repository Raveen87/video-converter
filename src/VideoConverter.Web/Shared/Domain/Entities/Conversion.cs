using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Shared.Domain.Entities;

public class Conversion
{
    public int Id { get; set; }

    public string InputPath { get; set; } = string.Empty;

    public string OutputDirectory { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int? Crf { get; set; }

    public int? AudioBitrateKbps { get; set; }

    public AudioChannels? AudioChannels { get; set; }

    public int? Resolution { get; set; }

    public string? SelectedAudioStreamIndicesCsv { get; set; }

    public string? SelectedSubtitleStreamIndicesCsv { get; set; }

    public string? SelectedExternalSubtitlePathsCsv { get; set; }

    public string? DetectedCrop { get; set; } // format: "w:h:x:y"

    public string? MetadataBasedFilename { get; set; } // filename based on metadata lookup
}
