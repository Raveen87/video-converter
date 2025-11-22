namespace VideoConverter.Web.Models;

public class ConversionPatchDto
{
    public string? OutputDirectory { get; set; }

    public int? Crf { get; set; }

    public int? AudioBitrateKbps { get; set; }

    public AudioChannels? AudioChannels { get; set; }

    public int? Resolution { get; set; }

    public string? OutputFilenameTemplate { get; set; }

    public IReadOnlyList<string>? AudioLanguages { get; init; }

    public IReadOnlyList<string>? SubtitleLanguages { get; init; }
}
