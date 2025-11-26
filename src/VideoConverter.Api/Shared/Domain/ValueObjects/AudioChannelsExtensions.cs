using VideoConverter.Common.Models;

namespace VideoConverter.Api.Shared.Domain.ValueObjects;

public static class AudioChannelsExtensions
{
    public static int GetDefaultBitrate(this AudioChannels channels)
    {
        return channels switch
        {
            AudioChannels.Stereo20 => 128,
            AudioChannels.Surround51 => 192,
            _ => throw new ArgumentOutOfRangeException(nameof(channels), channels, "Unsupported audio channel configuration"),
        };
    }

    public static string GetFfmpegChannelLayout(this AudioChannels channels)
    {
        return channels switch
        {
            AudioChannels.Stereo20 => "stereo",
            AudioChannels.Surround51 => "5.1",
            _ => throw new ArgumentOutOfRangeException(nameof(channels), channels, "Unsupported audio channel configuration"),
        };
    }

    public static int GetChannelCount(this AudioChannels channels)
    {
        return channels switch
        {
            AudioChannels.Stereo20 => 2,
            AudioChannels.Surround51 => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(channels), channels, "Unsupported audio channel configuration"),
        };
    }
}
