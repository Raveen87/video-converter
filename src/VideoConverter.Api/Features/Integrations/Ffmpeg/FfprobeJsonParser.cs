using System.Text.Json;

using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class FfprobeJsonParser
{
    public static MediaProbeResult Parse(string stdout)
    {
        using var doc = JsonDocument.Parse(stdout);
        var (streams, width, height) = ParseStreams(doc);
        var duration = ParseDuration(doc);
        return new MediaProbeResult
        {
            Streams = streams,
            VideoWidth = width,
            VideoHeight = height,
            DurationSeconds = duration,
        };
    }

    private static (List<MediaStream> Streams, int? Width, int? Height) ParseStreams(JsonDocument doc)
    {
        var streams = new List<MediaStream>();
        int? width = null;
        int? height = null;
        if (doc.RootElement.TryGetProperty("streams", out var streamsEl) && streamsEl.ValueKind is JsonValueKind.Array)
        {
            foreach (var s in streamsEl.EnumerateArray())
            {
                var codecType = s.TryGetProperty("codec_type", out var ct) ? ct.GetString() : null;
                if (string.IsNullOrEmpty(codecType))
                {
                    continue;
                }

                var index = s.TryGetProperty("index", out var idxEl) ? idxEl.GetInt32() : -1;
                var codecName = s.TryGetProperty("codec_name", out var cnEl) ? cnEl.GetString() : null;
                var (lang, title) = ParseTags(s);

                if (string.Equals(codecType, "video", StringComparison.Ordinal))
                {
                    if (s.TryGetProperty("width", out var wEl) && wEl.ValueKind is JsonValueKind.Number)
                    {
                        width = wEl.GetInt32();
                    }

                    if (s.TryGetProperty("height", out var hEl) && hEl.ValueKind is JsonValueKind.Number)
                    {
                        height = hEl.GetInt32();
                    }
                }

                streams.Add(new MediaStream { Index = index, CodecType = codecType!, LanguageTag = lang, Title = title, CodecName = codecName });
            }
        }

        return (streams, width, height);
    }

    private static (string? Language, string? Title) ParseTags(JsonElement s)
    {
        string? lang = null;
        string? title = null;
        if (s.TryGetProperty("tags", out var tags) && tags.ValueKind is JsonValueKind.Object)
        {
            if (tags.TryGetProperty("language", out var langEl) && langEl.ValueKind is JsonValueKind.String)
            {
                lang = langEl.GetString();
            }

            if (tags.TryGetProperty("title", out var titleEl) && titleEl.ValueKind is JsonValueKind.String)
            {
                title = titleEl.GetString();
            }
        }

        return (lang, title);
    }

    private static double? ParseDuration(JsonDocument doc)
    {
        return doc.RootElement.TryGetProperty("format", out var formatEl) && formatEl.ValueKind is JsonValueKind.Object &&
               formatEl.TryGetProperty("duration", out var durEl) && durEl.ValueKind is JsonValueKind.String &&
               double.TryParse(durEl.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dur)
            ? dur
            : null;
    }
}
