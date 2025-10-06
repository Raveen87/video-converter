using VideoConverter.Web.Dtos;

namespace VideoConverter.Web.Validation;

public sealed class ConversionCreateValidator : IRequestValidator<ConversionCreateDto>
{
    public Task<ValidationResult> ValidateAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            result.Add(nameof(request.InputPath), "InputPath is required.");
        }
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            result.Add(nameof(request.OutputDirectory), "OutputDirectory is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.InputPath))
        {
            try
            {
                var full = Path.GetFullPath(request.InputPath);
                if (!File.Exists(full))
                {
                    result.Add(nameof(request.InputPath), "Input file does not exist.");
                }
                else
                {
                    var attr = File.GetAttributes(full);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        result.Add(nameof(request.InputPath), "InputPath must be a file, not a directory.");
                    }
                }
            }
            catch (Exception ex)
            {
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
                        result.Add(nameof(request.OutputDirectory), $"Cannot create output directory: {ex.Message}");
                    }
                }

                if (Directory.Exists(fullDir))
                {
                    var canWrite = CanWrite(fullDir);
                    if (!canWrite)
                    {
                        result.Add(nameof(request.OutputDirectory), "OutputDirectory is not writable.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add(nameof(request.OutputDirectory), $"Invalid path: {ex.Message}");
            }
        }

        // Validate CRF range when provided
        if (request.Crf is < 0 or > 63)
        {
            result.Add(nameof(request.Crf), "CRF must be between 0 and 63.");
        }

        // Validate audio bitrate range when provided (kbps)
        if (request.AudioBitrateKbps is <= 0 or > 450)
        {
            result.Add(nameof(request.AudioBitrateKbps), "AudioBitrateKbps must be between 1 and 450.");
        }

        // Validate audio language names format (when provided)
        if (request.AudioLanguages is { Count: > 0 })
        {
            foreach (var lang in request.AudioLanguages)
            {
                if (string.IsNullOrWhiteSpace(lang))
                {
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
                    result.Add(nameof(request.SubtitleLanguages), "Language names must be non-empty.");
                    break;
                }
            }
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


