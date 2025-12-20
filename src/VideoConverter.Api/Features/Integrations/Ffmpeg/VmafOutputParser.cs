namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Parses VMAF score from FFmpeg libvmaf filter JSON output.
/// </summary>
public sealed class VmafOutputParser(ILogger<VmafOutputParser> logger) : IVmafOutputParser
{
    private readonly ILogger<VmafOutputParser> _logger = logger;

    /// <inheritdoc />
    public double? Parse(string ffmpegOutput)
    {
        if (string.IsNullOrWhiteSpace(ffmpegOutput))
        {
            _logger.LogWarning("VMAF output is empty or whitespace");
            return null;
        }

        try
        {
            var jsonStart = ffmpegOutput.IndexOf('{');
            if (jsonStart < 0)
            {
                _logger.LogWarning("No JSON object found in VMAF output");
                return null;
            }

            var jsonEnd = ffmpegOutput.LastIndexOf('}');
            if (jsonEnd < jsonStart)
            {
                _logger.LogWarning("Malformed JSON in VMAF output - no closing brace found");
                return null;
            }

            var jsonContent = ffmpegOutput.Substring(jsonStart, jsonEnd - jsonStart + 1);

            using var doc = JsonDocument.Parse(jsonContent);
            return ExtractVmafScore(doc.RootElement);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse VMAF JSON output");
            return null;
        }
    }

    private double? ExtractVmafScore(JsonElement root)
    {
        if (root.TryGetProperty("pooled_metrics", out var pooledMetrics) &&
            pooledMetrics.TryGetProperty("vmaf", out var vmaf))
        {
            if (vmaf.TryGetProperty("mean", out var mean) &&
                mean.ValueKind is JsonValueKind.Number)
            {
                var score = mean.GetDouble();
                _logger.LogDebug("Parsed VMAF score from pooled_metrics.vmaf.mean: {Score}", score);
                return score;
            }

            if (vmaf.TryGetProperty("harmonic_mean", out var harmonicMean) &&
                harmonicMean.ValueKind is JsonValueKind.Number)
            {
                var score = harmonicMean.GetDouble();
                _logger.LogDebug("Parsed VMAF score from pooled_metrics.vmaf.harmonic_mean: {Score}", score);
                return score;
            }
        }

        if (root.TryGetProperty("VMAF score", out var vmafScore) &&
            vmafScore.ValueKind is JsonValueKind.Number)
        {
            var score = vmafScore.GetDouble();
            _logger.LogDebug("Parsed VMAF score from root 'VMAF score': {Score}", score);
            return score;
        }

        _logger.LogWarning("Could not find VMAF score in JSON output");
        return null;
    }
}
