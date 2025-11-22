using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class ConversionService(
    IMediaProber prober,
    IAudioLanguageMatcher audioMatcher,
    ISubtitleLanguageMatcher subtitleMatcher,
    ICropDetector cropDetector,
    IMetadataService metadataService,
    IFilenameParser filenameParser,
    IFilenameGenerator filenameGenerator,
    IFilenameTemplateEngine filenameTemplateEngine,
    IOptions<FilenameTemplateOptions> filenameTemplateOptions,
    ILogger<ConversionService> logger,
    IOptions<MetadataFetchOptions>? metadataFetchOptions = null,
    Shared.Infrastructure.Persistence.AppDbContext? db = null) : IConversionService
{
    private readonly IOptions<MetadataFetchOptions> _metadataFetchOptions = metadataFetchOptions ?? Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.DefaultToInputName });
    private readonly Shared.Infrastructure.Persistence.AppDbContext? _db = db;

    public async Task<Conversion> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating conversion for input: {InputPath}, output: {OutputDirectory}",
            request.InputPath,
            request.OutputDirectory);

        // Process audio language requirements
        var audioProcessing = await ProcessAudioLanguagesAsync(request, cancellationToken).ConfigureAwait(false);

        // Process subtitle language requirements
        var subtitleProcessing = await ProcessSubtitleLanguagesAsync(request, cancellationToken).ConfigureAwait(false);

        // Validate resolution against source
        await ValidateResolutionAsync(request, cancellationToken).ConfigureAwait(false);

        // Perform crop detection
        var detectedCrop = await DetectCropAsync(request, cancellationToken).ConfigureAwait(false);

        // Look up metadata and generate filename
        var metadataBasedFilename = await GenerateMetadataBasedFilenameAsync(request.InputPath, request.OutputFilenameTemplate ?? filenameTemplateOptions.Value.DefaultTemplate, cancellationToken)
            .ConfigureAwait(false);

        // Determine audio bitrate based on audio channels
        var audioBitrate = DetermineAudioBitrate(request.AudioBitrateKbps, request.AudioChannels);

        var conversion = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Status = Shared.Domain.ValueObjects.ConversionStatus.Pending,
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

        logger.LogInformation(
            "Conversion created with settings: CRF={Crf}, AudioBitrate={AudioBitrate}Kbps, Resolution={Resolution}p, AudioStreams={AudioStreams}, SubtitleStreams={SubtitleStreams}, ExternalSubtitles={ExternalSubtitles}, Crop={Crop}",
            conversion.Crf,
            conversion.AudioBitrateKbps,
            conversion.Resolution,
            audioProcessing.AudioIndicesCsv ?? "none",
            subtitleProcessing.SubtitleIndicesCsv ?? "none",
            subtitleProcessing.ExternalSubtitlePathsCsv ?? "none",
            detectedCrop ?? "none");

        return conversion;
    }

    public async Task<LanguageProcessingResult> ProcessLanguagesAsync(
        string inputPath,
        IEnumerable<string>? audioLanguages,
        IEnumerable<string>? subtitleLanguages,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Processing languages for existing conversion. Input: {InputPath}, Audio: {AudioLanguages}, Subtitle: {SubtitleLanguages}",
            inputPath,
            audioLanguages is not null ? string.Join(", ", audioLanguages) : "none",
            subtitleLanguages is not null ? string.Join(", ", subtitleLanguages) : "none");

        var audioResult = await ProcessAudioLanguagesForUpdateAsync(inputPath, audioLanguages, cancellationToken).ConfigureAwait(false);
        var subtitleResult = await ProcessSubtitleLanguagesForUpdateAsync(inputPath, subtitleLanguages, cancellationToken).ConfigureAwait(false);

        return new LanguageProcessingResult
        {
            AudioIndicesCsv = audioResult.AudioIndicesCsv,
            SubtitleIndicesCsv = subtitleResult.SubtitleIndicesCsv,
            ExternalSubtitlePathsCsv = subtitleResult.ExternalSubtitlePathsCsv,
        };
    }

    private static int DetermineAudioBitrate(int? requestedBitrate, AudioChannels? audioChannels)
    {
        // If bitrate is explicitly provided, use it
        if (requestedBitrate is not null)
        {
            return requestedBitrate.Value;
        }

        // Otherwise, use default bitrate based on audio channels
        var channels = audioChannels ?? AudioChannels.Stereo20;
        return channels.GetDefaultBitrate();
    }

    private static string? FindExternalSubtitle(string inputPath, string language)
    {
        var videoDir = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var videoBase = Path.GetFileNameWithoutExtension(inputPath);
        var tagsToTry = GetSubtitleTags(language);

        return tagsToTry.Select(tag => Path.Combine(videoDir, videoBase + "." + tag + ".srt"))
            .FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> GetSubtitleTags(string language)
    {
        var aliases = LanguageMatchEngine.GetAliasesForLanguage(language);
        return aliases;
    }

    private async Task<string?> GenerateMetadataBasedFilenameAsync(string inputPath, string? template, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Generating metadata-based filename for: {InputPath}", inputPath);

            // Parse the filename to extract media information
            var filename = Path.GetFileName(inputPath);
            var parsedFilename = filenameParser.ParseFilename(filename);
            var extension = Path.GetExtension(inputPath) ?? string.Empty;

            var metadata = await GetMetadataAsync(parsedFilename, cancellationToken).ConfigureAwait(false);

            if (metadata is not null)
            {
                var generatedFilename = template is not null
                    ? filenameTemplateEngine.Render(metadata, extension, template)
                    : filenameGenerator.GenerateFilename(metadata, extension);
                logger.LogInformation("Generated metadata-based filename: {GeneratedFilename}", generatedFilename);
                return generatedFilename;
            }

            return await HandleFetchFailureAsync(inputPath, filename, parsedFilename, extension, template, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Propagate explicit skip/queue signals
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate metadata-based filename for: {InputPath}, using original filename", inputPath);
            return Path.GetFileName(inputPath);
        }
    }

    private async Task<MediaMetadata?> GetMetadataAsync(ParsedFilename parsedFilename, CancellationToken cancellationToken)
    {
        if (parsedFilename.DetectedType is MediaType.Movie)
        {
            logger.LogDebug(
                "Looking up movie metadata for: {Title} (Year: {Year})",
                parsedFilename.Title,
                parsedFilename.Year);

            return await metadataService.GetMovieMetadataAsync(
                parsedFilename.Title ?? "Unknown",
                parsedFilename.Year,
                parsedFilename.Source,
                cancellationToken).ConfigureAwait(false);
        }

        if (parsedFilename.DetectedType is MediaType.TvShow)
        {
            logger.LogDebug(
                "Looking up TV show metadata for: {ShowName} S{Season}E{Episode}",
                parsedFilename.Title,
                parsedFilename.Season,
                parsedFilename.Episode);

            return await metadataService.GetTvShowMetadataAsync(
                parsedFilename.Title ?? "Unknown",
                parsedFilename.Season ?? 1,
                parsedFilename.Episode ?? 1,
                firstAiredYear: null,
                parsedFilename.Source,
                cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<string?> HandleFetchFailureAsync(string inputPath, string filename, ParsedFilename parsedFilename, string extension, string? template, CancellationToken cancellationToken)
    {
        switch (_metadataFetchOptions.Value.OnFailureBehavior)
        {
            case FetchFailureBehavior.DefaultToInputName:
                return RenderDefaultToInputNameFilename(filename, parsedFilename, extension, template);

            case FetchFailureBehavior.QueueForManualResolution:
                await QueueForManualResolutionAsync(inputPath, filename, parsedFilename, cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("Metadata fetch failed and item queued for manual resolution.");

            case FetchFailureBehavior.SkipItem:
                {
                    logger.LogWarning("No metadata found for: {InputPath}, skipping item as per configuration", inputPath);
                    throw new InvalidOperationException("Metadata fetch failed and item skipped by configuration.");
                }

            default:
                logger.LogError("Unknown fetch-failure behavior: {Behavior}. Failing for: {InputPath}", _metadataFetchOptions.Value.OnFailureBehavior, inputPath);
                throw new InvalidOperationException("Unknown fetch-failure behavior encountered in HandleFetchFailureAsync.");
        }
    }

    private string RenderDefaultToInputNameFilename(string filename, ParsedFilename parsedFilename, string extension, string? template)
    {
        logger.LogWarning("No metadata found for: {Filename}, defaulting to input name/template", filename);
        if (parsedFilename.DetectedType is MediaType.Movie)
        {
            var movie = new MovieMetadata(parsedFilename.Title ?? Path.GetFileNameWithoutExtension(filename), parsedFilename.Year ?? 0, parsedFilename.Source);
            return template is not null
                ? filenameTemplateEngine.Render(movie, extension, template)
                : filename;
        }

        var tv = new TvShowMetadata(
            parsedFilename.Title ?? Path.GetFileNameWithoutExtension(filename),
            FirstAiredYear: 0,
            parsedFilename.Season ?? 1,
            parsedFilename.Episode ?? 1,
            parsedFilename.EpisodeTitle,
            parsedFilename.Source);
        return template is not null
            ? filenameTemplateEngine.Render(tv, extension, template)
            : filename;
    }

    private async Task QueueForManualResolutionAsync(string inputPath, string filename, ParsedFilename parsedFilename, CancellationToken cancellationToken)
    {
        logger.LogWarning("No metadata found for: {InputPath}, queuing for manual resolution and skipping item", inputPath);
        if (_db is null)
        {
            throw new InvalidOperationException("Cannot queue for manual resolution because the database context (_db) is not available.");
        }

        var now = DateTime.UtcNow;
        var item = new MetadataResolutionQueueItem
        {
            SourcePath = inputPath,
            SourceFileName = filename,
            InputKind = parsedFilename.DetectedType.ToString(),
            ParsedTitle = parsedFilename.Title,
            ParsedYear = parsedFilename.Year,
            ParsedSeason = parsedFilename.Season,
            ParsedEpisode = parsedFilename.Episode,
            FailureReason = "MetadataNotFound",
            FailureDetails = "TMDb search failed or returned no results, or episode not found in series",
            Status = "Pending",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        _ = _db.MetadataResolutionQueue.Add(item);
        try
        {
            _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 })
        {
            logger.LogDebug(ex, "Item already queued for manual resolution: {InputPath}", inputPath);
        }
    }

    private async Task<AudioProcessingResult> ProcessAudioLanguagesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        var requested = request.AudioLanguages?.ToList();

        if (requested is not { Count: > 0 })
        {
            return new AudioProcessingResult();
        }

        logger.LogDebug(
            "Processing audio language requirements: {AudioLanguages}",
            string.Join(", ", requested));

        var probe = await prober.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
        var matched = audioMatcher.MatchAudioStreams(probe.Streams, requested);
        var audioIndicesCsv = string.Join(',', matched);

        logger.LogInformation(
            "Matched audio streams: {MatchedIndices} for languages: {RequestedLanguages}",
            audioIndicesCsv,
            string.Join(", ", requested));

        return new AudioProcessingResult { AudioIndicesCsv = audioIndicesCsv };
    }

    private async Task<SubtitleProcessingResult> ProcessSubtitleLanguagesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        var requested = request.SubtitleLanguages?.ToList();

        if (requested is not { Count: > 0 })
        {
            return await ProcessDefaultTextSubtitlesAsync(request, cancellationToken).ConfigureAwait(false);
        }

        logger.LogDebug(
            "Processing subtitle language requirements: {SubtitleLanguages}",
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
                logger.LogDebug(
                    "Found subtitle stream for language '{Language}': indices {Indices}",
                    lang,
                    string.Join(',', idx));
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

        var subtitleIndicesCsv = matchedIndices.Count > 0 ? string.Join(',', matchedIndices) : null;
        var externalSubtitlePathsCsv = externalPaths.Count > 0 ? string.Join('|', externalPaths) : null;

        logger.LogInformation(
            "Subtitle processing complete - Stream indices: {StreamIndices}, External files: {ExternalFiles}",
            subtitleIndicesCsv ?? "none",
            externalSubtitlePathsCsv ?? "none");

        return new SubtitleProcessingResult
        {
            SubtitleIndicesCsv = subtitleIndicesCsv,
            ExternalSubtitlePathsCsv = externalSubtitlePathsCsv,
        };
    }

    private async Task<SubtitleProcessingResult> ProcessDefaultTextSubtitlesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        // No specific languages requested: include all available text-based internal subtitles.
        var probeAll = await prober.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
        var streams = probeAll?.Streams ?? [];
        var textSubtitleIndices = streams
            .Where(s =>
                string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase)
                && SubtitleCodecHelpers.IsTextSubtitle(s.CodecName))
            .Select(s => s.Index)
            .ToList();

        var indicesCsv = textSubtitleIndices.Count > 0 ? string.Join(',', textSubtitleIndices) : null;

        logger.LogInformation(
            "No subtitle languages specified; including text-based subtitle streams: {Indices}",
            indicesCsv ?? "none");

        return new SubtitleProcessingResult
        {
            SubtitleIndicesCsv = indicesCsv,
            ExternalSubtitlePathsCsv = null,
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
                logger.LogWarning(
                    "Resolution validation failed: requested {RequestedResolution}p exceeds source {SourceHeight}p",
                    request.Resolution,
                    probe.VideoHeight);
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

    private async Task<AudioProcessingResult> ProcessAudioLanguagesForUpdateAsync(string inputPath, IEnumerable<string>? requestedLanguages, CancellationToken cancellationToken)
    {
        var requested = requestedLanguages?.ToList();

        if (requested is not { Count: > 0 })
        {
            return new AudioProcessingResult();
        }

        logger.LogDebug(
            "Processing audio language requirements: {AudioLanguages}",
            string.Join(", ", requested));

        var probe = await prober.ProbeAsync(inputPath, cancellationToken).ConfigureAwait(false);
        var matched = audioMatcher.MatchAudioStreams(probe.Streams, requested);
        var audioIndicesCsv = string.Join(',', matched);

        logger.LogInformation(
            "Matched audio streams: {MatchedIndices} for languages: {RequestedLanguages}",
            audioIndicesCsv,
            string.Join(", ", requested));

        return new AudioProcessingResult { AudioIndicesCsv = audioIndicesCsv };
    }

    private async Task<SubtitleProcessingResult> ProcessSubtitleLanguagesForUpdateAsync(string inputPath, IEnumerable<string>? requestedLanguages, CancellationToken cancellationToken)
    {
        var requested = requestedLanguages?.ToList();

        if (requested is not { Count: > 0 })
        {
            return await ProcessDefaultTextSubtitlesForUpdateAsync(inputPath, cancellationToken).ConfigureAwait(false);
        }

        logger.LogDebug(
            "Processing subtitle language requirements: {SubtitleLanguages}",
            string.Join(", ", requested));

        var probe = await prober.ProbeAsync(inputPath, cancellationToken).ConfigureAwait(false);
        var matchedIndices = new List<int>();
        var externalPaths = new List<string>();

        foreach (var lang in requested)
        {
            try
            {
                var idx = subtitleMatcher.MatchSubtitleStreams(probe.Streams, [lang]);
                matchedIndices.AddRange(idx);
                logger.LogDebug(
                    "Found subtitle stream for language '{Language}': indices {Indices}",
                    lang,
                    string.Join(',', idx));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Subtitle language '{Language}' not found in streams, checking external files", lang);

                var externalPath = FindExternalSubtitle(inputPath, lang);
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

        var subtitleIndicesCsv = matchedIndices.Count > 0 ? string.Join(',', matchedIndices) : null;
        var externalSubtitlePathsCsv = externalPaths.Count > 0 ? string.Join('|', externalPaths) : null;

        logger.LogInformation(
            "Subtitle processing complete - Stream indices: {StreamIndices}, External files: {ExternalFiles}",
            subtitleIndicesCsv ?? "none",
            externalSubtitlePathsCsv ?? "none");

        return new SubtitleProcessingResult
        {
            SubtitleIndicesCsv = subtitleIndicesCsv,
            ExternalSubtitlePathsCsv = externalSubtitlePathsCsv,
        };
    }

    private async Task<SubtitleProcessingResult> ProcessDefaultTextSubtitlesForUpdateAsync(string inputPath, CancellationToken cancellationToken)
    {
        // No specific languages requested: include all available text-based internal subtitles.
        var probeAll = await prober.ProbeAsync(inputPath, cancellationToken).ConfigureAwait(false);
        var streams = probeAll?.Streams ?? [];
        var textSubtitleIndices = streams
            .Where(s =>
                string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase)
                && SubtitleCodecHelpers.IsTextSubtitle(s.CodecName))
            .Select(s => s.Index)
            .ToList();

        var indicesCsv = textSubtitleIndices.Count > 0 ? string.Join(',', textSubtitleIndices) : null;
        logger.LogInformation("Including all text-based subtitles: {Indices}", indicesCsv ?? "none");

        return new SubtitleProcessingResult
        {
            SubtitleIndicesCsv = indicesCsv,
            ExternalSubtitlePathsCsv = null,
        };
    }

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
