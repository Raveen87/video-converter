using VideoConverter.Web.Models;

namespace VideoConverter.Web.Data;

public class Profile
{
    public int Id { get; set; }
    public string? OutputDirectory { get; set; }
    public int? Crf { get; set; }
    public int? AudioBitrateKbps { get; set; }
    public AudioChannels? AudioChannels { get; set; }
    public int? Resolution { get; set; }
    public string? MetadataBasedFilename { get; set; }
}
