using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Features.Metadata.Models;
using VideoConverter.Web.Features.Metadata.Services;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Conversions.Services;

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
    public async Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating conversion for input: {InputPath}, output: {OutputDirectory}",
            request.InputPath, request.OutputDirectory);

        // Process audio language requirements
        var audioProcessing = await ProcessAudioLanguagesAsync(request, cancellationToken).ConfigureAwait(false);

        // Process subtitle language requirements
        var subtitleProcessing = await ProcessSubtitleLanguagesAsync(request, cancellationToken).ConfigureAwait(false);

        // Validate resolution against source
        await ValidateResolutionAsync(request, cancellationToken).ConfigureAwait(false);

        // Perform crop detection
        var detectedCrop = await DetectCropAsync(request, cancellationToken).ConfigureAwait(false);

        // Look up metadata and generate filename
        var metadataBasedFilename = await GenerateMetadataBasedFilenameAsync(request.InputPath, cancellationToken).ConfigureAwait(false);

        // Determine audio bitrate based on audio channels
        var audioBitrate = DetermineAudioBitrate(request.AudioBitrateKbps, request.AudioChannels);

        var conversion = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Crf = request.Crf,
            AudioBitrateKbps = audioBitrate,
            AudioChannels = request.AudioChannels ?? AudioChannels.Stereo20, // Default to 2.0
            Resolution = request.Resolution,
            SelectedAudioStreamIndicesCsv = audioProcessing.AudioIndicesCsv,
            SelectedSubtitleStreamIndicesCsv = subtitleProcessing.SubtitleIndicesCsv,
            SelectedExternalSubtitlePathsCsv = subtitleProcessing.ExternalSubtitlePathsCsv,
            DetectedCrop = detectedCrop,
            MetadataBasedFilename = metadataBasedFilename,
        };

        logger.LogInformation("Conversion created with settings: CRF={Crf}, AudioBitrate={AudioBitrate}Kbps, Resolution={Resolution}p, AudioStreams={AudioStreams}, SubtitleStreams={SubtitleStreams}, ExternalSubtitles={ExternalSubtitles}, Crop={Crop}",
            conversion.Crf, conversion.AudioBitrateKbps, conversion.Resolution,
            audioProcessing.AudioIndicesCsv ?? "none", subtitleProcessing.SubtitleIndicesCsv ?? "none",
            subtitleProcessing.ExternalSubtitlePathsCsv ?? "none", detectedCrop ?? "none");

        return conversion;
    }

    private static int DetermineAudioBitrate(int? requestedBitrate, AudioChannels? audioChannels)
    {
        // If bitrate is explicitly provided, use it
        if (requestedBitrate.HasValue)
        {
            return requestedBitrate.Value;
        }

        // Otherwise, use default bitrate based on audio channels
        var channels = audioChannels ?? AudioChannels.Stereo20;
        return channels.GetDefaultBitrate();
    }

    private async Task<string?> GenerateMetadataBasedFilenameAsync(string inputPath, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Generating metadata-based filename for: {InputPath}", inputPath);

            // Parse the filename to extract media information
            var filename = Path.GetFileName(inputPath);
            var parsedFilename = filenameParser.ParseFilename(filename);
            var extension = Path.GetExtension(inputPath);

            MediaMetadata? metadata = null;

            // Look up metadata based on parsed information
            if (parsedFilename.DetectedType == MediaType.Movie)
            {
                logger.LogDebug("Looking up movie metadata for: {Title} (Year: {Year})",
                    parsedFilename.Title, parsedFilename.Year);

                metadata = await metadataService.GetMovieMetadataAsync(
                    parsedFilename.Title ?? "Unknown",
                    parsedFilename.Year,
                    parsedFilename.Source,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (parsedFilename.DetectedType == MediaType.TvShow)
            {
                logger.LogDebug("Looking up TV show metadata for: {ShowName} S{Season}E{Episode}",
                    parsedFilename.Title, parsedFilename.Season, parsedFilename.Episode);

                metadata = await metadataService.GetTvShowMetadataAsync(
                    parsedFilename.Title ?? "Unknown",
                    parsedFilename.Season ?? 1,
                    parsedFilename.Episode ?? 1,
                    firstAiredYear: null, // We don't have first aired year from filename parsing
                    parsedFilename.Source,
                    cancellationToken).ConfigureAwait(false);
            }

            if (metadata is not null)
            {
                var generatedFilename = filenameGenerator.GenerateFilename(metadata, extension);
                logger.LogInformation("Generated metadata-based filename: {GeneratedFilename}", generatedFilename);
                return generatedFilename;
            }

            logger.LogWarning("No metadata found for: {InputPath}, using original filename", inputPath);
            return filename;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate metadata-based filename for: {InputPath}, using original filename", inputPath);
            return Path.GetFileName(inputPath);
        }
    }

    private async Task<AudioProcessingResult> ProcessAudioLanguagesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        var requested = request.AudioLanguages?.ToList();

        if (requested is not { Count: > 0 })
        {
            return new AudioProcessingResult();
        }

        logger.LogDebug("Processing audio language requirements: {AudioLanguages}",
            string.Join(", ", requested));

        var probe = await prober.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
        var matched = audioMatcher.MatchAudioStreams(probe.Streams, requested);
        var audioIndicesCsv = string.Join(",", matched);

        logger.LogInformation("Matched audio streams: {MatchedIndices} for languages: {RequestedLanguages}",
            audioIndicesCsv, string.Join(", ", requested));

        return new AudioProcessingResult { AudioIndicesCsv = audioIndicesCsv };
    }

    private async Task<SubtitleProcessingResult> ProcessSubtitleLanguagesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        var requested = request.SubtitleLanguages?.ToList();

        if (requested is not { Count: > 0 })
        {
            return new SubtitleProcessingResult();
        }

        logger.LogDebug("Processing subtitle language requirements: {SubtitleLanguages}",
            string.Join(", ", requested));

        var probe = await prober.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
        var matchedIndices = new List<int>();
        var externalPaths = new List<string>();

        foreach (var lang in requested)
        {
            try
            {
                var idx = subtitleMatcher.MatchSubtitleStreams(probe.Streams, [lang]);
                matchedIndices.AddRange(idx);
                logger.LogDebug("Found subtitle stream for language '{Language}': indices {Indices}",
                    lang, string.Join(",", idx));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Subtitle language '{Language}' not found in streams, checking external files", lang);

                var externalPath = FindExternalSubtitle(request.InputPath, lang);
                if (externalPath is not null)
                {
                    externalPaths.Add(externalPath);
                    logger.LogDebug("Found external subtitle file: {Path}", externalPath);
                }
                else
                {
                    logger.LogWarning("Subtitle language '{Language}' not found in file or as external .srt", lang);
                    throw new InvalidOperationException($"Requested subtitle language '{lang}' not found in file or as external .srt.");
                }
            }
        }

        var subtitleIndicesCsv = matchedIndices.Count > 0 ? string.Join(",", matchedIndices) : null;
        var externalSubtitlePathsCsv = externalPaths.Count > 0 ? string.Join('|', externalPaths) : null;

        logger.LogInformation("Subtitle processing complete - Stream indices: {StreamIndices}, External files: {ExternalFiles}",
            subtitleIndicesCsv ?? "none", externalSubtitlePathsCsv ?? "none");

        return new SubtitleProcessingResult
        {
            SubtitleIndicesCsv = subtitleIndicesCsv,
            ExternalSubtitlePathsCsv = externalSubtitlePathsCsv,
        };
    }

    private async Task ValidateResolutionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        if (request.Resolution is null)
        {
            return;
        }

        logger.LogDebug("Validating resolution {RequestedResolution}p against source", request.Resolution);

        try
        {
            var probe = await prober.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
            if (probe.VideoHeight is not null && request.Resolution > probe.VideoHeight)
            {
                logger.LogWarning("Resolution validation failed: requested {RequestedResolution}p exceeds source {SourceHeight}p",
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
            logger.LogDebug(ex, "Could not determine source height: {Error}", ex.Message);
            // Continue without validation if we can't determine source height
        }
    }

    private async Task<string?> DetectCropAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting crop detection for {InputPath}", request.InputPath);

        try
        {
            var probe = await prober.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
            var detectedCrop = await cropDetector.DetectAsync(request.InputPath, probe.DurationSeconds, cancellationToken).ConfigureAwait(false);

            if (detectedCrop is not null)
            {
                logger.LogInformation("Crop detection completed: {DetectedCrop}", detectedCrop);
            }
            else
            {
                logger.LogDebug("No crop detected for {InputPath}", request.InputPath);
            }

            return detectedCrop;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Crop detection failed: {Error}", ex.Message);
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
        var key = language.Trim().ToLowerInvariant();

        return key switch
        {
            _ when SubtitleTagsSwedish.Contains(key, StringComparer.Ordinal) => SubtitleTagsSwedish,
            _ when SubtitleTagsEnglish.Contains(key, StringComparer.Ordinal) => SubtitleTagsEnglish,
            _ when SubtitleTagsVietnamese.Contains(key, StringComparer.Ordinal) => SubtitleTagsVietnamese,
            _ => [key], // Fallback: if a tag-like short code is provided, just try it
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
