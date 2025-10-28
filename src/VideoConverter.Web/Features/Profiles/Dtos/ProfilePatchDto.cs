using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Profiles.Dtos;

public class ProfilePatchDto
{
    public string? OutputDirectory { get; set; }

    public int? Crf { get; set; }

    public int? AudioBitrateKbps { get; set; }

    public AudioChannels? AudioChannels { get; set; }

    public int? Resolution { get; set; }

    public string? MetadataBasedFilename { get; set; }

    public IEnumerable<string>? AudioLanguages { get; init; }

    public IEnumerable<string>? SubtitleLanguages { get; init; }
}
