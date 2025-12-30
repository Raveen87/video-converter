using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Shared.Infrastructure.Serialization;

[JsonSerializable(typeof(MediaProbeResult))]
[JsonSerializable(typeof(IEnumerable<ConversionDto>))]
[JsonSerializable(typeof(ConversionDto))]
[JsonSerializable(typeof(ConversionCreateDto))]
[JsonSerializable(typeof(IEnumerable<ProfileDto>))]
[JsonSerializable(typeof(ProfileDto))]
[JsonSerializable(typeof(DirectoryQueueRequestDto))]
[JsonSerializable(typeof(FileBrowserResponseDto))]
[JsonSerializable(typeof(FileItemDto))]
[JsonSerializable(typeof(IEnumerable<FileItemDto>))]
[JsonSerializable(typeof(QueueReorderRequestDto))]
[JsonSerializable(typeof(ConversionStatus))]
[JsonSerializable(typeof(ConversionPatchDto))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(TmdbJson.TmdbMovieSearchResult))]
[JsonSerializable(typeof(TmdbJson.TmdbTvSearchResult))]
[JsonSerializable(typeof(TmdbJson.TmdbEpisodeDetails))]
[JsonSerializable(typeof(TmdbJson.TmdbTvShowDetailsResponse))]
[JsonSerializable(typeof(TmdbJson.TmdbSeasonDetailsResponse))]
[JsonSerializable(typeof(TmdbJson.TmdbEpisodeGroupsResponse))]
[JsonSerializable(typeof(TmdbJson.TmdbEpisodeGroupDetailResponse))]
[JsonSerializable(typeof(MediaStreamDto))]
[JsonSerializable(typeof(List<MediaStreamDto>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
