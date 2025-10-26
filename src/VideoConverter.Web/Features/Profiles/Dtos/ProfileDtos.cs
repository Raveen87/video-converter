using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Profiles.Dtos;

public record ProfileCreateDto(
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename);

public record ProfileDto(
    int Id,
    string? OutputDirectory,
    int? Crf,
    int? AudioBitrateKbps,
    AudioChannels? AudioChannels,
    int? Resolution,
    string? MetadataBasedFilename);

public class ProfilePatchDto
{
    public string? OutputDirectory { get; set; }
    public int? Crf { get; set; }
    public int? AudioBitrateKbps { get; set; }
    public AudioChannels? AudioChannels { get; set; }
    public int? Resolution { get; set; }
    public string? MetadataBasedFilename { get; set; }
}
