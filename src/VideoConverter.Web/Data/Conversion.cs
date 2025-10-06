namespace VideoConverter.Web.Data;

public class Conversion
{
    public int Id { get; set; }
    public string InputPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int? Crf { get; set; }
    public int? AudioBitrateKbps { get; set; }
    public string? SelectedAudioStreamIndicesCsv { get; set; }
    public string? SelectedSubtitleStreamIndicesCsv { get; set; }
}

