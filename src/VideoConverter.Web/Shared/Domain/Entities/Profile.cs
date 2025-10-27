using System.Collections.Generic;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Shared.Domain.Entities;

public class Profile
{
    public int Id { get; set; }
    public string? OutputDirectory { get; set; }
    public int? Crf { get; set; }
    public int? AudioBitrateKbps { get; set; }
    public AudioChannels? AudioChannels { get; set; }
    public int? Resolution { get; set; }
    public string? MetadataBasedFilename { get; set; }
    public List<string>? AudioLanguages { get; set; }
    public List<string>? SubtitleLanguages { get; set; }
}
