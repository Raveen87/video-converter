using VideoConverter.Web.Dtos;

namespace VideoConverter.Web.Validation;

public sealed class ConversionCreateValidator(ILogger<ConversionCreateValidator> logger) : IRequestValidator<ConversionCreateDto>
{
    private readonly ILogger<ConversionCreateValidator> _logger = logger;

    public Task<ValidationResult> ValidateAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting validation for conversion request");
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            _logger.LogWarning("Validation failed: InputPath is required");
            result.Add(nameof(request.InputPath), "InputPath is required.");
        }
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            _logger.LogWarning("Validation failed: OutputDirectory is required");
            result.Add(nameof(request.OutputDirectory), "OutputDirectory is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.InputPath))
        {
            try
            {
                var full = Path.GetFullPath(request.InputPath);
                if (!File.Exists(full))
                {
                    _logger.LogWarning("Validation failed: Input file does not exist: {InputPath}", full);
                    result.Add(nameof(request.InputPath), "Input file does not exist.");
                }
                else
                {
                    var attr = File.GetAttributes(full);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        _logger.LogWarning("Validation failed: InputPath is a directory, not a file: {InputPath}", full);
                        result.Add(nameof(request.InputPath), "InputPath must be a file, not a directory.");
                    }
                    else
                    {
                        _logger.LogDebug("Input file validation passed: {InputPath}", full);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Validation failed: Invalid input path: {InputPath}", request.InputPath);
                result.Add(nameof(request.InputPath), $"Invalid path: {ex.Message}");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            try
            {
                var fullDir = Path.GetFullPath(request.OutputDirectory);
                // Ensure directory exists or can be created and is writable
                if (!Directory.Exists(fullDir))
                {
                    try
                    {
                        _ = Directory.CreateDirectory(fullDir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Validation failed: Cannot create output directory: {OutputDirectory}", fullDir);
                        result.Add(nameof(request.OutputDirectory), $"Cannot create output directory: {ex.Message}");
                    }
                }

                if (Directory.Exists(fullDir))
                {
                    var canWrite = CanWrite(fullDir);
                    if (!canWrite)
                    {
                        _logger.LogWarning("Validation failed: Output directory is not writable: {OutputDirectory}", fullDir);
                        result.Add(nameof(request.OutputDirectory), "OutputDirectory is not writable.");
                    }
                    else
                    {
                        _logger.LogDebug("Output directory validation passed: {OutputDirectory}", fullDir);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Validation failed: Invalid output directory path: {OutputDirectory}", request.OutputDirectory);
                result.Add(nameof(request.OutputDirectory), $"Invalid path: {ex.Message}");
            }
        }

        // Validate CRF range when provided
        if (request.Crf is < 0 or > 63)
        {
            _logger.LogWarning("Validation failed: CRF value {Crf} is out of range (0-63)", request.Crf);
            result.Add(nameof(request.Crf), "CRF must be between 0 and 63.");
        }

        // Validate audio bitrate range when provided (kbps)
        if (request.AudioBitrateKbps is <= 0 or > 450)
        {
            _logger.LogWarning("Validation failed: Audio bitrate {AudioBitrate}Kbps is out of range (1-450)", request.AudioBitrateKbps);
            result.Add(nameof(request.AudioBitrateKbps), "AudioBitrateKbps must be between 1 and 450.");
        }

        // Validate resolution (when provided): allowed values 720, 1080, 2160
        if (request.Resolution is not null)
        {
            var allowed = new[] { 720, 1080, 2160 };
            if (!allowed.Contains(request.Resolution.Value))
            {
                _logger.LogWarning("Validation failed: Resolution {Resolution}p is not allowed (must be 720, 1080, or 2160)", request.Resolution);
                result.Add(nameof(request.Resolution), "Resolution must be one of: 720, 1080, 2160.");
            }
        }

        // Validate audio language names format (when provided)
        if (request.AudioLanguages is { Count: > 0 })
        {
            foreach (var lang in request.AudioLanguages)
            {
                if (string.IsNullOrWhiteSpace(lang))
                {
                    _logger.LogWarning("Validation failed: Empty audio language name provided");
                    result.Add(nameof(request.AudioLanguages), "Language names must be non-empty.");
                    break;
                }
            }
        }

        // Validate subtitle language names format (when provided)
        if (request.SubtitleLanguages is { Count: > 0 })
        {
            foreach (var lang in request.SubtitleLanguages)
            {
                if (string.IsNullOrWhiteSpace(lang))
                {
                    _logger.LogWarning("Validation failed: Empty subtitle language name provided");
                    result.Add(nameof(request.SubtitleLanguages), "Language names must be non-empty.");
                    break;
                }
            }
        }

        if (result.IsValid)
        {
            _logger.LogInformation("Validation completed successfully");
        }
        else
        {
            _logger.LogWarning("Validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }

    private static bool CanWrite(string directory)
    {
        try
        {
            var testFile = Path.Combine(directory, ".write_test_" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}


