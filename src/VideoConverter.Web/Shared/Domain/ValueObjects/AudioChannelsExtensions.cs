namespace VideoConverter.Web.Shared.Domain.ValueObjects;

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
}
