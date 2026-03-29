using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class ConversionService(
    IMediaProber prober,
    IAudioLanguageMatcher audioMatcher,
    ISubtitleLanguageMatcher subtitleMatcher,
    ICropDetector cropDetector,
    IGrainDetector grainDetector,
    IMetadataService metadataService,
    IFilenameParser filenameParser,
    IFilenameGenerator filenameGenerator,
    IFilenameTemplateEngine filenameTemplateEngine,
    IOptions<FilenameTemplateOptions> filenameTemplateOptions,
    ILogger<ConversionService> logger,
    IOptions<MetadataFetchOptions>? metadataFetchOptions = null,
    IOptions<StreamSelectionOptions>? streamSelectionOptions = null,
    Shared.Infrastructure.Persistence.AppDbContext? db = null,
    INotificationService? notificationService = null) : IConversionService
{
    private readonly IOptions<MetadataFetchOptions> _metadataFetchOptions = metadataFetchOptions ?? Options.Create(new MetadataFetchOptions { OnFailureBehavior = FetchFailureBehavior.DefaultToInputName });
    private readonly IOptions<StreamSelectionOptions> _streamSelectionOptions = streamSelectionOptions ?? Options.Create(new StreamSelectionOptions { OnFailureBehavior = StreamSelectionFailureBehavior.QueueForManualResolution });
    private readonly Shared.Infrastructure.Persistence.AppDbContext? _db = db;
    private readonly INotificationService? _notificationService = notificationService;

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

        var probeTask = prober.ProbeAsync(request.InputPath, cancellationToken);

        // Validate resolution against source
        await ValidateResolutionAsync(request, probeTask).ConfigureAwait(false);

        // Perform crop detection
        var detectedCrop = await DetectCropAsync(request, probeTask, cancellationToken).ConfigureAwait(false);

        // Perform grain detection
        var detectedGrainLevel = await DetectGrainLevelAsync(request, probeTask, cancellationToken).ConfigureAwait(false);

        // Look up metadata and generate filename
        var (metadataBasedFilename, metadata) = await GenerateMetadataBasedFilenameAsync(request, cancellationToken).ConfigureAwait(false);

        // Determine audio bitrate based on audio channels
        var audioBitrate = DetermineAudioBitrate(request.AudioBitrateKbps, request.AudioChannels);

        var conversion = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Crf = request.Crf,
            AudioBitrateKbps = audioBitrate,
            AudioChannels = request.AudioChannels ?? AudioChannels.Stereo20, // Default to 2.0
            Resolution = request.Resolution,
            SelectedAudioStreamIndicesCsv = audioProcessing.AudioIndicesCsv,
            SelectedSubtitleStreamIndicesCsv = subtitleProcessing.SubtitleIndicesCsv,
            SelectedExternalSubtitlePathsCsv = subtitleProcessing.ExternalSubtitlePathsCsv,
            DetectedCrop = detectedCrop,
            DetectedGrainLevel = detectedGrainLevel,
            MetadataBasedFilename = metadataBasedFilename,
            TmdbId = metadata?.TmdbId,
            OutputPathTemplate = request.OutputPathTemplate,
            VmafEnabled = request.VmafEnabled ?? false,
            RequiredVmaf = request.RequiredVmaf,
        };

        if (metadata is not null)
        {
            UpdateConversionWithMetadata(conversion, metadata);
        }

        LogConversionCreated(conversion, audioProcessing, subtitleProcessing, detectedCrop, detectedGrainLevel);
        return conversion;
    }

    public async Task<Conversion> CreateConversionWithMetadataAsync(ConversionCreateDto request, MediaMetadata metadata, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating conversion with forced metadata for input: {InputPath}, output: {OutputDirectory}, MetadataTmdbId={TmdbId}",
            request.InputPath,
            request.OutputDirectory,
            metadata.TmdbId);

        // Process audio language requirements
        var audioProcessing = await ProcessAudioLanguagesAsync(request, cancellationToken).ConfigureAwait(false);

        // Process subtitle language requirements
        var subtitleProcessing = await ProcessSubtitleLanguagesAsync(request, cancellationToken).ConfigureAwait(false);

        var probeTask = prober.ProbeAsync(request.InputPath, cancellationToken);

        // Validate resolution against source
        await ValidateResolutionAsync(request, probeTask).ConfigureAwait(false);

        // Perform crop detection
        var detectedCrop = await DetectCropAsync(request, probeTask, cancellationToken).ConfigureAwait(false);

        // Perform grain detection
        var detectedGrainLevel = await DetectGrainLevelAsync(request, probeTask, cancellationToken).ConfigureAwait(false);

        // Generate filename using provided metadata
        var metadataBasedFilename = GenerateFilenameFromMetadata(request, metadata);
        logger.LogInformation("Generated metadata-based filename (forced): {GeneratedFilename}", metadataBasedFilename);

        // Determine audio bitrate based on audio channels
        var audioBitrate = DetermineAudioBitrate(request.AudioBitrateKbps, request.AudioChannels);

        var conversion = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Crf = request.Crf,
            AudioBitrateKbps = audioBitrate,
            AudioChannels = request.AudioChannels ?? AudioChannels.Stereo20, // Default to 2.0
            Resolution = request.Resolution,
            SelectedAudioStreamIndicesCsv = audioProcessing.AudioIndicesCsv,
            SelectedSubtitleStreamIndicesCsv = subtitleProcessing.SubtitleIndicesCsv,
            SelectedExternalSubtitlePathsCsv = subtitleProcessing.ExternalSubtitlePathsCsv,
            DetectedCrop = detectedCrop,
            DetectedGrainLevel = detectedGrainLevel,
            MetadataBasedFilename = metadataBasedFilename,
            TmdbId = metadata.TmdbId,
            OutputPathTemplate = request.OutputPathTemplate,
            VmafEnabled = request.VmafEnabled ?? false,
            RequiredVmaf = request.RequiredVmaf,
        };

        UpdateConversionWithMetadata(conversion, metadata);
        LogConversionCreated(conversion, audioProcessing, subtitleProcessing, detectedCrop, detectedGrainLevel);
        return conversion;
    }

    public async Task<Conversion> UpdateMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken)
    {
        if (_db is null)
        {
            throw new InvalidOperationException("Database context is not available.");
        }

        var conversion = await _db.Conversions.FindAsync([id], cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Conversion with ID {id} not found.");

        if (conversion.Status is not ConversionStatus.Pending)
        {
            throw ApiErrorException.BusinessError(
                ErrorCode.InvalidConversionState,
                "Can only update metadata for pending conversions.",
                $"Conversion {id} is in status {conversion.Status} and cannot be modified.");
        }

        logger.LogInformation("Updating metadata for conversion {ConversionId} to TmdbId {TmdbId} ({MediaType})", id, request.TmdbId, request.MediaType);

        // Re-parse filename to get S/E info if needed
        var filename = Path.GetFileName(conversion.InputPath);
        var parsedFilename = filenameParser.ParseFilename(filename);

        var metadata = await FetchMetadataForUpdateAsync(request, parsedFilename, conversion, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Could not fetch metadata for TmdbId {request.TmdbId} ({request.MediaType}).");

        // Update conversion fields
        conversion.TmdbId = metadata.TmdbId;
        UpdateConversionWithMetadata(conversion, metadata);

        // Regenerate filename using default template
        var extension = Path.GetExtension(conversion.InputPath) ?? string.Empty;
        var template = filenameTemplateOptions.Value.DefaultTemplate;

        conversion.MetadataBasedFilename = template is not null
            ? filenameTemplateEngine.Render(metadata, extension, template)
            : filenameGenerator.GenerateFilename(metadata, extension);

        _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully updated metadata for conversion {ConversionId}.", id);
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

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Recursively check for SqliteException with ErrorCode 19
        var current = ex.InnerException;
        while (current is not null)
        {
            if (current is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private static void UpdateConversionWithMetadata(Conversion conversion, MediaMetadata metadata)
    {
        if (metadata is MovieMetadata movie)
        {
            conversion.MatchedTitle = movie.Title;
            conversion.MatchedYear = movie.Year;
            conversion.MatchedSeason = null;
            conversion.MatchedEpisode = null;
        }
        else if (metadata is TvShowMetadata tv)
        {
            conversion.MatchedTitle = tv.ShowName;
            conversion.MatchedYear = tv.FirstAiredYear;
            conversion.MatchedSeason = tv.Season;
            conversion.MatchedEpisode = tv.Episode;
        }
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

    private static (string Language, bool IsOptional) ParseLanguageRequest(string request)
    {
        var trimmed = request.Trim();
        if (trimmed.EndsWith('?'))
        {
            return (trimmed[..^1].TrimEnd(), true);
        }

        return (trimmed, false);
    }

    private static List<MediaStreamDto> MapToStreamDtos(IReadOnlyList<MediaStream> streams)
    {
        return
        [
            .. streams.Select(s => new MediaStreamDto
            {
                Index = s.Index,
                CodecType = s.CodecType,
                CodecName = s.CodecName,
                LanguageTag = s.LanguageTag,
                Title = s.Title,
            }),
        ];
    }

    private List<string> FindMissingAudioLanguages(IReadOnlyList<MediaStream> audioStreams, List<string> requestedLanguages)
    {
        var missing = new List<string>();
        foreach (var language in requestedLanguages)
        {
            try
            {
                _ = audioMatcher.MatchAudioStreams(audioStreams, [language]);
            }
            catch
            {
                missing.Add(language);
            }
        }

        return missing;
    }

    private async Task<MediaMetadata?> FetchMetadataForUpdateAsync(ResolveMetadataRequestDto request, ParsedFilename parsedFilename, Conversion conversion, CancellationToken cancellationToken)
    {
        if (request.MediaType is MediaType.Movie)
        {
            return await metadataService.GetMovieMetadataByIdAsync(request.TmdbId, parsedFilename.Source, cancellationToken).ConfigureAwait(false);
        }

        if (request.MediaType is MediaType.TvShow)
        {
            // Use existing match S/E if available, otherwise parse from filename, otherwise default to 1
            var season = conversion.MatchedSeason ?? parsedFilename.Season ?? 1;
            var episode = conversion.MatchedEpisode ?? parsedFilename.Episode ?? 1;

            return await metadataService.GetTvShowMetadataByIdAsync(request.TmdbId, season, episode, parsedFilename.Source, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private string GenerateFilenameFromMetadata(ConversionCreateDto request, MediaMetadata metadata)
    {
        var extension = Path.GetExtension(request.InputPath) ?? string.Empty;
        var template = request.OutputFilenameTemplate ?? filenameTemplateOptions.Value.DefaultTemplate;
        return template is not null
            ? filenameTemplateEngine.Render(metadata, extension, template)
            : filenameGenerator.GenerateFilename(metadata, extension);
    }

    private async Task<(string? Filename, MediaMetadata? Metadata)> GenerateMetadataBasedFilenameAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Generating metadata-based filename for: {InputPath}", request.InputPath);

            // Parse the filename to extract media information
            var filename = Path.GetFileName(request.InputPath);
            var parsedFilename = filenameParser.ParseFilename(filename);
            var extension = Path.GetExtension(request.InputPath) ?? string.Empty;
            var template = request.OutputFilenameTemplate ?? filenameTemplateOptions.Value.DefaultTemplate;

            var metadata = await GetMetadataAsync(parsedFilename, cancellationToken).ConfigureAwait(false);

            if (metadata is not null)
            {
                var generatedFilename = template is not null
                    ? filenameTemplateEngine.Render(metadata, extension, template)
                    : filenameGenerator.GenerateFilename(metadata, extension);
                logger.LogInformation("Generated metadata-based filename: {GeneratedFilename}", generatedFilename);
                return (generatedFilename, metadata);
            }

            return await HandleFetchFailureAsync(request, filename, parsedFilename, extension, template, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Propagate explicit skip/queue signals
            throw;
        }
        catch (ApiErrorException)
        {
            // Propagate API errors (e.g. queue/skip signals mapped to business errors)
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate metadata-based filename for: {InputPath}, using original filename", request.InputPath);
            return (Path.GetFileName(request.InputPath), null);
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

    private async Task<(string? Filename, MediaMetadata? Metadata)> HandleFetchFailureAsync(ConversionCreateDto request, string filename, ParsedFilename parsedFilename, string extension, string? template, CancellationToken cancellationToken)
    {
        switch (_metadataFetchOptions.Value.OnFailureBehavior)
        {
            case FetchFailureBehavior.DefaultToInputName:
                return RenderDefaultToInputNameFilename(filename, parsedFilename, extension, template);

            case FetchFailureBehavior.QueueForManualResolution:
                await QueueForManualResolutionAsync(request, filename, parsedFilename, cancellationToken).ConfigureAwait(false);
                if (_notificationService is not null)
                {
                    _ = await _notificationService.AddAsync(
                        "Metadata Lookup Failed",
                        $"Input: {filename}. Item added to Metadata Resolution Queue.",
                        NotificationType.Warning,
                        cancellationToken).ConfigureAwait(false);
                }

                throw ApiErrorException.BusinessError(
                    ErrorCode.InvalidConversionState,
                    "Metadata fetch failed and item queued for manual resolution.",
                    "Metadata fetch failed and item queued for manual resolution.");

            case FetchFailureBehavior.SkipItem:
                {
                    logger.LogWarning("No metadata found for: {InputPath}, skipping item as per configuration", request.InputPath);
                    throw new InvalidOperationException("Metadata fetch failed and item skipped by configuration.");
                }

            default:
                logger.LogError("Unknown fetch-failure behavior: {Behavior}. Failing for: {InputPath}", _metadataFetchOptions.Value.OnFailureBehavior, request.InputPath);
                throw new InvalidOperationException("Unknown fetch-failure behavior encountered in HandleFetchFailureAsync.");
        }
    }

    private (string Filename, MediaMetadata Metadata) RenderDefaultToInputNameFilename(string filename, ParsedFilename parsedFilename, string extension, string? template)
    {
        logger.LogWarning("No metadata found for: {Filename}, defaulting to input name/template", filename);
        if (parsedFilename.DetectedType is MediaType.Movie)
        {
            var movie = new MovieMetadata(parsedFilename.Title ?? Path.GetFileNameWithoutExtension(filename), parsedFilename.Year ?? 0, parsedFilename.Source);
            var result = template is not null
                ? filenameTemplateEngine.Render(movie, extension, template)
                : filename;
            return (result, movie);
        }

        var tv = new TvShowMetadata(
            parsedFilename.Title ?? Path.GetFileNameWithoutExtension(filename),
            FirstAiredYear: 0,
            parsedFilename.Season ?? 1,
            parsedFilename.Episode ?? 1,
            parsedFilename.EpisodeTitle,
            parsedFilename.Source);
        var resultTv = template is not null
            ? filenameTemplateEngine.Render(tv, extension, template)
            : filename;
        return (resultTv, tv);
    }

    private async Task QueueForManualResolutionAsync(ConversionCreateDto request, string filename, ParsedFilename parsedFilename, CancellationToken cancellationToken)
    {
        logger.LogWarning("No metadata found for: {InputPath}, queuing for manual resolution and skipping item", request.InputPath);
        if (_db is null)
        {
            throw new InvalidOperationException("Cannot queue for manual resolution because the database context (_db) is not available.");
        }

        // Check if item is already in the queue to avoid unique constraint violation
        var existing = await _db.ManualResolutionQueue
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.SourcePath == request.InputPath && q.ResolutionType == ManualResolutionType.Metadata && q.Status == "Pending", cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            logger.LogInformation("Item is already in manual resolution queue: {InputPath}", request.InputPath);
            return;
        }

        var now = DateTime.UtcNow;
        var item = new ManualResolutionQueueItem
        {
            SourcePath = request.InputPath,
            SourceFileName = filename,
            ResolutionType = ManualResolutionType.Metadata,
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
            SerializedRequest = JsonSerializer.Serialize(request),
        };
        _ = _db.ManualResolutionQueue.Add(item);

        try
        {
            _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            // Fallback: If race condition occurred and item was added between check and save, swallow the error
            if (IsUniqueConstraintViolation(ex))
            {
                logger.LogDebug(ex, "Item already queued for manual resolution (caught race condition): {InputPath}", request.InputPath);
            }
            else
            {
                throw;
            }
        }
    }

    private async Task QueueForStreamSelectionAsync(
        ConversionCreateDto request,
        IReadOnlyList<MediaStream> audioStreams,
        IReadOnlyList<MediaStream> subtitleStreams,
        IEnumerable<string>? missingAudioLanguages,
        IEnumerable<string>? missingSubtitleLanguages,
        string failureReason,
        string failureDetails,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("Stream selection failed for: {InputPath}, queuing for manual resolution", request.InputPath);
        if (_db is null)
        {
            throw new InvalidOperationException("Cannot queue for manual resolution because the database context (_db) is not available.");
        }

        if (await IsAlreadyQueuedForStreamSelectionAsync(request.InputPath, cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Item is already in manual resolution queue for stream selection: {InputPath}", request.InputPath);
            return;
        }

        var item = BuildStreamSelectionQueueItem(request, audioStreams, subtitleStreams, missingAudioLanguages, missingSubtitleLanguages, failureReason, failureDetails);
        _ = _db.ManualResolutionQueue.Add(item);

        try
        {
            _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            if (IsUniqueConstraintViolation(ex))
            {
                logger.LogDebug(ex, "Item already queued for stream selection (caught race condition): {InputPath}", request.InputPath);
            }
            else
            {
                throw;
            }
        }
    }

    private async Task<bool> IsAlreadyQueuedForStreamSelectionAsync(string inputPath, CancellationToken cancellationToken)
    {
        var existing = await _db!.ManualResolutionQueue
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.SourcePath == inputPath && q.ResolutionType == ManualResolutionType.StreamSelection && q.Status == "Pending", cancellationToken)
            .ConfigureAwait(false);
        return existing is not null;
    }

    private ManualResolutionQueueItem BuildStreamSelectionQueueItem(
        ConversionCreateDto request,
        IReadOnlyList<MediaStream> audioStreams,
        IReadOnlyList<MediaStream> subtitleStreams,
        IEnumerable<string>? missingAudioLanguages,
        IEnumerable<string>? missingSubtitleLanguages,
        string failureReason,
        string failureDetails)
    {
        var filename = Path.GetFileName(request.InputPath);
        var parsedFilename = filenameParser.ParseFilename(filename);
        var now = DateTime.UtcNow;

        return new ManualResolutionQueueItem
        {
            SourcePath = request.InputPath,
            SourceFileName = filename,
            ResolutionType = ManualResolutionType.StreamSelection,
            InputKind = parsedFilename.DetectedType.ToString(),
            ParsedTitle = parsedFilename.Title,
            ParsedYear = parsedFilename.Year,
            ParsedSeason = parsedFilename.Season,
            ParsedEpisode = parsedFilename.Episode,
            FailureReason = failureReason,
            FailureDetails = failureDetails,
            Status = "Pending",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SerializedRequest = JsonSerializer.Serialize(request),
            AvailableAudioStreamsJson = JsonSerializer.Serialize(MapToStreamDtos(audioStreams)),
            AvailableSubtitleStreamsJson = JsonSerializer.Serialize(MapToStreamDtos(subtitleStreams)),
            MissingAudioLanguages = missingAudioLanguages is not null ? string.Join(',', missingAudioLanguages) : null,
            MissingSubtitleLanguages = missingSubtitleLanguages is not null ? string.Join(',', missingSubtitleLanguages) : null,
        };
    }

    private async Task<AudioProcessingResult> ProcessAudioLanguagesAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        var probe = await prober.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
        var inputAudioStreams = probe.Streams
            .Where(s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (inputAudioStreams.Count is 0)
        {
            logger.LogWarning("No audio streams found in input file: {InputPath}", request.InputPath);
            throw new InvalidOperationException($"No audio streams found in input file {request.InputPath}.");
        }

        var requested = request.AudioLanguages?.ToList();
        if (requested is not { Count: > 0 })
        {
            return new AudioProcessingResult();
        }

        logger.LogDebug(
            "Processing audio language requirements: {AudioLanguages}",
            string.Join(", ", requested));

        try
        {
            var matched = audioMatcher.MatchAudioStreams(probe.Streams, requested);

            if (matched.Count is 0)
            {
                return await HandleAudioStreamSelectionFailureAsync(
                    request,
                    probe.Streams,
                    requested,
                    "No matching audio streams found",
                    cancellationToken).ConfigureAwait(false);
            }

            var audioIndicesCsv = string.Join(',', matched);

            logger.LogInformation(
                "Matched audio streams: {MatchedIndices} for languages: {RequestedLanguages}",
                audioIndicesCsv,
                string.Join(", ", requested));

            return new AudioProcessingResult { AudioIndicesCsv = audioIndicesCsv };
        }
        catch (ApiErrorException ex) when (ex.ErrorCode is ErrorCode.ValidationFailed)
        {
            return await HandleAudioStreamSelectionFailureAsync(
                request,
                probe.Streams,
                requested,
                ex.Message,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<AudioProcessingResult> HandleAudioStreamSelectionFailureAsync(
        ConversionCreateDto request,
        IReadOnlyList<MediaStream> allStreams,
        List<string> requestedLanguages,
        string failureDetails,
        CancellationToken cancellationToken)
    {
        var audioStreams = allStreams.Where(s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase)).ToList();
        var subtitleStreams = allStreams.Where(s => string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase)).ToList();

        // Determine which specific languages are actually missing
        var missingLanguages = FindMissingAudioLanguages(audioStreams, requestedLanguages);

        switch (_streamSelectionOptions.Value.OnFailureBehavior)
        {
            case StreamSelectionFailureBehavior.Fail:
                logger.LogWarning(
                    "Audio stream selection failed for languages: {MissingLanguages} in {InputPath}",
                    string.Join(", ", missingLanguages),
                    request.InputPath);
                throw new InvalidOperationException($"No matching audio streams found for languages: {string.Join(", ", missingLanguages)}");

            case StreamSelectionFailureBehavior.QueueForManualResolution:
                await QueueForStreamSelectionAsync(
                    request,
                    audioStreams,
                    subtitleStreams,
                    missingLanguages,
                    missingSubtitleLanguages: null,
                    "AudioLanguageNotFound",
                    failureDetails,
                    cancellationToken).ConfigureAwait(false);

                if (_notificationService is not null)
                {
                    var filename = Path.GetFileName(request.InputPath);
                    _ = await _notificationService.AddAsync(
                        "Audio Stream Selection Failed",
                        $"Input: {filename}. Item added to Manual Resolution Queue.",
                        NotificationType.Warning,
                        cancellationToken).ConfigureAwait(false);
                }

                throw ApiErrorException.BusinessError(
                    ErrorCode.InvalidConversionState,
                    "Audio stream selection failed and item queued for manual resolution.",
                    "Audio stream selection failed and item queued for manual resolution.");

            case StreamSelectionFailureBehavior.SkipItem:
                logger.LogWarning("Audio stream selection failed for: {InputPath}, skipping item as per configuration", request.InputPath);
                throw new InvalidOperationException("Audio stream selection failed and item skipped by configuration.");

            default:
                logger.LogError("Unknown stream selection failure behavior: {Behavior}", _streamSelectionOptions.Value.OnFailureBehavior);
                throw new InvalidOperationException("Unknown stream selection failure behavior.");
        }
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

        try
        {
            var (matchedIndices, externalPaths, missingLanguages) = await ProcessSubtitleLanguageListAsync(request.InputPath, requested, probe.Streams).ConfigureAwait(false);

            if (missingLanguages.Count > 0)
            {
                return await HandleSubtitleStreamSelectionFailureAsync(
                    request,
                    probe.Streams,
                    missingLanguages,
                    $"Subtitle languages not found: {string.Join(", ", missingLanguages)}",
                    cancellationToken).ConfigureAwait(false);
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("subtitle language"))
        {
            // Extract missing language from the error message
            var missingLanguages = requested.Where(r => ex.Message.Contains(ParseLanguageRequest(r).Language)).ToList();
            return await HandleSubtitleStreamSelectionFailureAsync(
                request,
                probe.Streams,
                missingLanguages,
                ex.Message,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<(List<int> MatchedIndices, List<string> ExternalPaths, List<string> MissingLanguages)> ProcessSubtitleLanguageListAsync(
        string inputPath,
        List<string> requested,
        IReadOnlyList<MediaStream> streams)
    {
        var matchedIndices = new List<int>();
        var externalPaths = new List<string>();
        var missingLanguages = new List<string>();

        foreach (var lang in requested)
        {
            var (language, isOptional) = ParseLanguageRequest(lang);
            try
            {
                var idx = subtitleMatcher.MatchSubtitleStreams(streams, [language]);
                matchedIndices.AddRange(idx);
                logger.LogDebug(
                    "Found subtitle stream for language '{Language}': indices {Indices}",
                    language,
                    string.Join(',', idx));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Subtitle language '{Language}' not found in streams, checking external files", language);

                var externalPath = FindExternalSubtitle(inputPath, language);
                if (externalPath is not null)
                {
                    externalPaths.Add(externalPath);
                    logger.LogDebug("Found external subtitle file: {Path}", externalPath);
                }
                else
                {
                    if (isOptional)
                    {
                        logger.LogInformation("Subtitle language '{Language}' not found in file or as external .srt (optional, skipping)", language);
                        continue;
                    }

                    logger.LogWarning("Subtitle language '{Language}' not found in file or as external .srt", language);
                    missingLanguages.Add(language);
                }
            }
        }

        return Task.FromResult((matchedIndices, externalPaths, missingLanguages));
    }

    private async Task<SubtitleProcessingResult> HandleSubtitleStreamSelectionFailureAsync(
        ConversionCreateDto request,
        IReadOnlyList<MediaStream> allStreams,
        List<string> missingLanguages,
        string failureDetails,
        CancellationToken cancellationToken)
    {
        var audioStreams = allStreams.Where(s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase)).ToList();
        var subtitleStreams = allStreams.Where(s => string.Equals(s.CodecType, "subtitle", StringComparison.OrdinalIgnoreCase)).ToList();

        switch (_streamSelectionOptions.Value.OnFailureBehavior)
        {
            case StreamSelectionFailureBehavior.Fail:
                logger.LogWarning(
                    "Subtitle stream selection failed for languages: {MissingLanguages} in {InputPath}",
                    string.Join(", ", missingLanguages),
                    request.InputPath);
                throw new InvalidOperationException($"Requested subtitle languages not found: {string.Join(", ", missingLanguages)}");

            case StreamSelectionFailureBehavior.QueueForManualResolution:
                await QueueForStreamSelectionAsync(
                    request,
                    audioStreams,
                    subtitleStreams,
                    missingAudioLanguages: null,
                    missingLanguages,
                    "SubtitleLanguageNotFound",
                    failureDetails,
                    cancellationToken).ConfigureAwait(false);

                if (_notificationService is not null)
                {
                    var filename = Path.GetFileName(request.InputPath);
                    _ = await _notificationService.AddAsync(
                        "Subtitle Stream Selection Failed",
                        $"Input: {filename}. Item added to Manual Resolution Queue.",
                        NotificationType.Warning,
                        cancellationToken).ConfigureAwait(false);
                }

                throw ApiErrorException.BusinessError(
                    ErrorCode.InvalidConversionState,
                    "Subtitle stream selection failed and item queued for manual resolution.",
                    "Subtitle stream selection failed and item queued for manual resolution.");

            case StreamSelectionFailureBehavior.SkipItem:
                logger.LogWarning("Subtitle stream selection failed for: {InputPath}, skipping item as per configuration", request.InputPath);
                throw new InvalidOperationException("Subtitle stream selection failed and item skipped by configuration.");

            default:
                logger.LogError("Unknown stream selection failure behavior: {Behavior}", _streamSelectionOptions.Value.OnFailureBehavior);
                throw new InvalidOperationException("Unknown stream selection failure behavior.");
        }
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

    private async Task ValidateResolutionAsync(ConversionCreateDto request, Task<MediaProbeResult> probeTask)
    {
        if (request.Resolution is null)
        {
            return;
        }

        logger.LogDebug("Validating resolution {RequestedResolution}p against source", request.Resolution);

        try
        {
            var probe = await probeTask.ConfigureAwait(false);
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

    private async Task<string?> DetectCropAsync(ConversionCreateDto request, Task<MediaProbeResult> probeTask, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting crop detection for {InputPath}", request.InputPath);

        try
        {
            var probe = await probeTask.ConfigureAwait(false);
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

    private async Task<GrainLevel> DetectGrainLevelAsync(ConversionCreateDto request, Task<MediaProbeResult> probeTask, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting grain detection for {InputPath}", request.InputPath);

        try
        {
            var probe = await probeTask.ConfigureAwait(false);
            return await grainDetector.DetectAsync(request.InputPath, probe.DurationSeconds, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Grain detection failed for {InputPath}. Falling back to {FallbackGrainLevel}",
                request.InputPath,
                GrainLevel.Low);
            return GrainLevel.Low;
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
        var (matchedIndices, externalPaths, _) = await ProcessSubtitleLanguageListAsync(inputPath, requested, probe.Streams).ConfigureAwait(false);

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

    private void LogConversionCreated(Conversion conversion, AudioProcessingResult audioProcessing, SubtitleProcessingResult subtitleProcessing, string? detectedCrop, GrainLevel detectedGrainLevel)
    {
        logger.LogInformation(
            "Conversion created with settings: CRF={Crf}, AudioBitrate={AudioBitrate}Kbps, Resolution={Resolution}p, AudioStreams={AudioStreams}, SubtitleStreams={SubtitleStreams}, ExternalSubtitles={ExternalSubtitles}, Crop={Crop}, GrainLevel={GrainLevel}",
            conversion.Crf,
            conversion.AudioBitrateKbps,
            conversion.Resolution,
            audioProcessing.AudioIndicesCsv ?? "none",
            subtitleProcessing.SubtitleIndicesCsv ?? "none",
            subtitleProcessing.ExternalSubtitlePathsCsv ?? "none",
            detectedCrop ?? "none",
            detectedGrainLevel);
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
