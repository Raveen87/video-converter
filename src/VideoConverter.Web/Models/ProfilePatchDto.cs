using System.Text.Json.Serialization;

namespace VideoConverter.Web.Models;

public class ProfilePatchDto
{
    public string? OutputDirectory { get; set; }

    public int? Crf { get; set; }

    public int? AudioBitrateKbps { get; set; }

    [JsonConverter(typeof(AudioChannelsConverter))]
    public string? AudioChannels { get; set; }

    public int? Resolution { get; set; }

    public string? MetadataBasedFilename { get; set; }

    public string? OutputFilenameTemplate { get; set; }

    public IReadOnlyList<string>? AudioLanguages { get; init; }

    public IReadOnlyList<string>? SubtitleLanguages { get; init; }
}
