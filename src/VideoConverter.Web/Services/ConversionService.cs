using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public sealed class ConversionService(
    IMediaProber prober,
    IAudioLanguageMatcher audioMatcher,
    ISubtitleLanguageMatcher subtitleMatcher,
    ICropDetector cropDetector,
    IMetadataService metadataService,
    IFilenameParser filenameParser,
    IFilenameGenerator filenameGenerator,
    ILogger<ConversionService> logger) : IConversionService
{
    private readonly IMediaProber _prober = prober;
    private readonly IAudioLanguageMatcher _audioMatcher = audioMatcher;
    private readonly ISubtitleLanguageMatcher _subtitleMatcher = subtitleMatcher;
    private readonly ICropDetector _cropDetector = cropDetector;
    private readonly IMetadataService _metadataService = metadataService;
    private readonly IFilenameParser _filenameParser = filenameParser;
    private readonly IFilenameGenerator _filenameGenerator = filenameGenerator;
    private readonly ILogger<ConversionService> _logger = logger;

    public async Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating conversion for input: {InputPath}, output: {OutputDirectory}",
            request.InputPath, request.OutputDirectory);

        // Process audio language requirements
        var audioProcessing = await ProcessAudioLanguagesAsync(request, cancellationToken);

        // Process subtitle language requirements
        var subtitleProcessing = await ProcessSubtitleLanguagesAsync(request, cancellationToken);

        // Validate resolution against source
        await ValidateResolutionAsync(request, cancellationToken);

        // Perform crop detection
        var detectedCrop = await DetectCropAsync(request, cancellationToken);

        // Look up metadata and generate filename
        var metadataBasedFilename = await GenerateMetadataBasedFilenameAsync(request.InputPath, cancellationToken);

        var conversion = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Crf = request.Crf,
            AudioBitrateKbps = request.AudioBitrateKbps,
            Resolution = request.Resolution,
            SelectedAudioStreamIndicesCsv = audioProcessing.AudioIndicesCsv,
            SelectedSubtitleStreamIndicesCsv = subtitleProcessing.SubtitleIndicesCsv,
            SelectedExternalSubtitlePathsCsv = subtitleProcessing.ExternalSubtitlePathsCsv,
            DetectedCrop = detectedCrop,
            MetadataBasedFilename = metadataBasedFilename
        };

        _logger.LogInformation("Conversion created with settings: CRF={Crf}, AudioBitrate={AudioBitrate}Kbps, Resolution={Resolution}p, AudioStreams={AudioStreams}, SubtitleStreams={SubtitleStreams}, ExternalSubtitles={ExternalSubtitles}, Crop={Crop}",
            conversion.Crf, conversion.AudioBitrateKbps, conversion.Resolution,
            audioProcessing.AudioIndicesCsv ?? "none", subtitleProcessing.SubtitleIndicesCsv ?? "none",
            subtitleProcessing.ExternalSubtitlePathsCsv ?? "none", detectedCrop ?? "none");

        return conversion;
    }

    private async Task<string?> GenerateMetadataBasedFilenameAsync(string inputPath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating metadata-based filename for: {InputPath}", inputPath);

            // Parse the filename to extract media information
            var filename = Path.GetFileName(inputPath);
            var parsedFilename = _filenameParser.ParseFilename(filename);
            var extension = Path.GetExtension(inputPath);

            MediaMetadata? metadata = null;

            // Look up metadata based on parsed information
            if (parsedFilename.DetectedType == MediaType.Movie)
            {
                _logger.LogDebug("Looking up movie metadata for: {Title} (Year: {Year})",
                    parsedFilename.Title, parsedFilename.Year);

                metadata = await _metadataService.GetMovieMetadataAsync(
                    parsedFilename.Title ?? "Unknown",
                    parsedFilename.Year,
                    cancellationToken);
            }
            else if (parsedFilename.DetectedType == MediaType.TvShow)
            {
                _logger.LogDebug("Looking up TV show metadata for: {ShowName} S{Season}E{Episode}",
                    parsedFilename.Title, parsedFilename.Season, parsedFilename.Episode);

                metadata = await _metadataService.GetTvShowMetadataAsync(
                    parsedFilename.Title ?? "Unknown",
                    parsedFilename.Season ?? 1,
                    parsedFilename.Episode ?? 1,
                    null, // We don't have first aired year from filename parsing
                    cancellationToken);
            }

            if (metadata is not null)
            {
                var generatedFilename = _filenameGenerator.GenerateFilename(metadata, extension);
                _logger.LogInformation("Generated metadata-based filename: {GeneratedFilename}", generatedFilename);
                return generatedFilename;
            }
            else
            {
                _logger.LogWarning("No metadata found for: {InputPath}, using original filename", inputPath);
                return filename;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate metadata-based filename for: {InputPath}, using original filename", inputPath);
            return Path.GetFileName(inputPath);
        }
    }

    private async Task<AudioProcessingResult> ProcessAudioLanguagesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        if (request.AudioLanguages is not { Count: > 0 })
        {
            return new AudioProcessingResult();
        }

        _logger.LogDebug("Processing audio language requirements: {AudioLanguages}",
            string.Join(", ", request.AudioLanguages));

        var probe = await _prober.ProbeAsync(request.InputPath, cancellationToken);
        var requested = request.AudioLanguages.ToList();
        var matched = _audioMatcher.MatchAudioStreams(probe.Streams, requested);
        var audioIndicesCsv = string.Join(",", matched);

        _logger.LogInformation("Matched audio streams: {MatchedIndices} for languages: {RequestedLanguages}",
            audioIndicesCsv, string.Join(", ", requested));

        return new AudioProcessingResult { AudioIndicesCsv = audioIndicesCsv };
    }

    private async Task<SubtitleProcessingResult> ProcessSubtitleLanguagesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        if (request.SubtitleLanguages is not { Count: > 0 })
        {
            return new SubtitleProcessingResult();
        }

        _logger.LogDebug("Processing subtitle language requirements: {SubtitleLanguages}",
            string.Join(", ", request.SubtitleLanguages));

        var probe = await _prober.ProbeAsync(request.InputPath, cancellationToken);
        var requested = request.SubtitleLanguages.ToList();
        var matchedIndices = new List<int>();
        var externalPaths = new List<string>();

        foreach (var lang in requested)
        {
            try
            {
                var idx = _subtitleMatcher.MatchSubtitleStreams(probe.Streams, [lang]);
                matchedIndices.AddRange(idx);
                _logger.LogDebug("Found subtitle stream for language '{Language}': indices {Indices}",
                    lang, string.Join(",", idx));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Subtitle language '{Language}' not found in streams, checking external files", lang);

                var externalPath = FindExternalSubtitle(request.InputPath, lang);
                if (externalPath is not null)
                {
                    externalPaths.Add(externalPath);
                    _logger.LogDebug("Found external subtitle file: {Path}", externalPath);
                }
                else
                {
                    _logger.LogWarning("Subtitle language '{Language}' not found in file or as external .srt", lang);
                    throw new InvalidOperationException($"Requested subtitle language '{lang}' not found in file or as external .srt.");
                }
            }
        }

        var subtitleIndicesCsv = matchedIndices.Count > 0 ? string.Join(",", matchedIndices) : null;
        var externalSubtitlePathsCsv = externalPaths.Count > 0 ? string.Join('|', externalPaths) : null;

        _logger.LogInformation("Subtitle processing complete - Stream indices: {StreamIndices}, External files: {ExternalFiles}",
            subtitleIndicesCsv ?? "none", externalSubtitlePathsCsv ?? "none");

        return new SubtitleProcessingResult
        {
            SubtitleIndicesCsv = subtitleIndicesCsv,
            ExternalSubtitlePathsCsv = externalSubtitlePathsCsv
        };
    }

    private async Task ValidateResolutionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        if (request.Resolution is null)
        {
            return;
        }

        _logger.LogDebug("Validating resolution {RequestedResolution}p against source", request.Resolution);

        try
        {
            var probe = await _prober.ProbeAsync(request.InputPath, cancellationToken);
            if (probe.VideoHeight is not null && request.Resolution > probe.VideoHeight)
            {
                _logger.LogWarning("Resolution validation failed: requested {RequestedResolution}p exceeds source {SourceHeight}p",
                    request.Resolution, probe.VideoHeight);
                throw new InvalidOperationException($"Requested resolution {request.Resolution}p exceeds source height {probe.VideoHeight}p.");
            }
        }
        catch (InvalidOperationException)
        {
            // Re-throw validation exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not determine source height: {Error}", ex.Message);
            // Continue without validation if we can't determine source height
        }
    }

    private async Task<string?> DetectCropAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting crop detection for {InputPath}", request.InputPath);

        try
        {
            var probe = await _prober.ProbeAsync(request.InputPath, cancellationToken);
            var detectedCrop = await _cropDetector.DetectAsync(request.InputPath, probe.DurationSeconds, cancellationToken);

            if (detectedCrop is not null)
            {
                _logger.LogInformation("Crop detection completed: {DetectedCrop}", detectedCrop);
            }
            else
            {
                _logger.LogDebug("No crop detected for {InputPath}", request.InputPath);
            }

            return detectedCrop;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Crop detection failed: {Error}", ex.Message);
            return null;
        }
    }

    private static string? FindExternalSubtitle(string inputPath, string language)
    {
        var videoDir = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var videoBase = Path.GetFileNameWithoutExtension(inputPath) ?? string.Empty;
        var tagsToTry = GetSubtitleTags(language);

        foreach (var tag in tagsToTry)
        {
            var path = Path.Combine(videoDir, videoBase + "." + tag + ".srt");
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetSubtitleTags(string language)
    {
        var key = (language ?? string.Empty).Trim().ToLowerInvariant();

        return key switch
        {
            var k when SubtitleTagsSwedish.Contains(k, StringComparer.Ordinal) => SubtitleTagsSwedish,
            var k when SubtitleTagsEnglish.Contains(k, StringComparer.Ordinal) => SubtitleTagsEnglish,
            var k when SubtitleTagsVietnamese.Contains(k, StringComparer.Ordinal) => SubtitleTagsVietnamese,
            _ => [key] // Fallback: if a tag-like short code is provided, just try it
        };
    }

    private static readonly string[] SubtitleTagsSwedish = ["sv", "sve", "swe", "swedish"];
    private static readonly string[] SubtitleTagsEnglish = ["en", "eng", "english"];
    private static readonly string[] SubtitleTagsVietnamese = ["vi", "vie", "viet", "vietnamese"];

    private sealed record AudioProcessingResult
    {
        public string? AudioIndicesCsv { get; init; }
    }

    private sealed record SubtitleProcessingResult
    {
        public string? SubtitleIndicesCsv { get; init; }
        public string? ExternalSubtitlePathsCsv { get; init; }
    }
}
