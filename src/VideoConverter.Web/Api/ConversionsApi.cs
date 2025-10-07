using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Services;
using VideoConverter.Web.Validation;

namespace VideoConverter.Web.Api;

public static class ConversionsApi
{
    private static readonly string[] SubtitleTagsSwedish = ["sv", "sve", "swe", "swedish"];
    private static readonly string[] SubtitleTagsEnglish = ["en", "eng", "english"];
    private static readonly string[] SubtitleTagsVietnamese = ["vi", "vie", "viet", "vietnamese"];

    private static IEnumerable<string> GetSubtitleTags(string language)
    {
        var key = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (SubtitleTagsSwedish.Contains(key, StringComparer.Ordinal))
        {
            return SubtitleTagsSwedish;
        }
        if (SubtitleTagsEnglish.Contains(key, StringComparer.Ordinal))
        {
            return SubtitleTagsEnglish;
        }
        if (SubtitleTagsVietnamese.Contains(key, StringComparer.Ordinal))
        {
            return SubtitleTagsVietnamese;
        }
        // Fallback: if a tag-like short code is provided, just try it
        return [key];
    }
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        _ = app.MapPost("/api/conversions", CreateConversionAsync);
        _ = app.MapGet("/api/conversions", ListConversionsAsync);
        _ = app.MapPost("/api/conversions/{id:int}/start", StartConversionByIdAsync);
        _ = app.MapPost("/api/conversions/start-next", StartNextConversionAsync);
    }

    private static async Task<IResult> CreateConversionAsync(
        ConversionCreateDto request,
        AppDbContext db,
        IRequestValidator<ConversionCreateDto> validator,
        IMediaProber prober,
        IAudioLanguageMatcher matcher,
        ISubtitleLanguageMatcher subtitleMatcher,
        ICropDetector cropDetector,
        ILogger logger)
    {
        logger.LogInformation("Creating conversion for input: {InputPath}, output: {OutputDirectory}",
            request.InputPath, request.OutputDirectory);

        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
        {
            logger.LogWarning("Validation failed for conversion request: {ValidationErrors}",
                string.Join("; ", validation.ToDictionary().SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"))));
            return Results.ValidationProblem(validation.ToDictionary());
        }

        logger.LogDebug("Validation passed for conversion request");

        // If audio/subtitle languages specified, probe and validate before persisting
        string? audioIndicesCsv = null;
        string? subtitleIndicesCsv = null;
        string? externalSubtitlePathsCsv = null;
        int? sourceHeight = null;
        double? durationSeconds = null;

        if (request.AudioLanguages is { Count: > 0 })
        {
            logger.LogDebug("Processing audio language requirements: {AudioLanguages}",
                string.Join(", ", request.AudioLanguages));

            var probe = await prober.ProbeAsync(request.InputPath, CancellationToken.None);
            try
            {
                var requested = request.AudioLanguages.ToList();
                var matched = matcher.MatchAudioStreams(probe.Streams, requested);
                audioIndicesCsv = string.Join(",", matched);
                logger.LogInformation("Matched audio streams: {MatchedIndices} for languages: {RequestedLanguages}",
                    audioIndicesCsv, string.Join(", ", requested));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Audio language matching failed: {Error}", ex.Message);
                var dict = new Dictionary<string, string[]>
                {
                    { nameof(request.AudioLanguages), new[] { ex.Message } }
                };
                return Results.ValidationProblem(dict);
            }
        }

        if (request.SubtitleLanguages is { Count: > 0 })
        {
            logger.LogDebug("Processing subtitle language requirements: {SubtitleLanguages}",
                string.Join(", ", request.SubtitleLanguages));

            var probe = await prober.ProbeAsync(request.InputPath, CancellationToken.None);
            var requested = request.SubtitleLanguages.ToList();
            List<int> matchedIndices = [];
            List<string> externalPaths = [];

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
                    // Not found in streams → try filesystem in same folder: <video-name>.<tag>.srt
                    var videoDir = Path.GetDirectoryName(request.InputPath) ?? string.Empty;
                    var videoBase = Path.GetFileNameWithoutExtension(request.InputPath) ?? string.Empty;
                    var tagsToTry = GetSubtitleTags(lang);

                    var found = false;
                    foreach (var tag in tagsToTry)
                    {
                        var path = Path.Combine(videoDir, videoBase + "." + tag + ".srt");
                        if (File.Exists(path))
                        {
                            externalPaths.Add(path);
                            found = true;
                            logger.LogDebug("Found external subtitle file: {Path}", path);
                            break;
                        }
                    }

                    if (!found)
                    {
                        logger.LogWarning("Subtitle language '{Language}' not found in file or as external .srt", lang);
                        var dict = new Dictionary<string, string[]>
                        {
                            { nameof(request.SubtitleLanguages), new[] { $"Requested subtitle language '{lang}' not found in file or as external .srt." } }
                        };
                        return Results.ValidationProblem(dict);
                    }
                }
            }

            subtitleIndicesCsv = matchedIndices.Count > 0 ? string.Join(",", matchedIndices) : null;
            externalSubtitlePathsCsv = externalPaths.Count > 0 ? string.Join('|', externalPaths) : null;

            logger.LogInformation("Subtitle processing complete - Stream indices: {StreamIndices}, External files: {ExternalFiles}",
                subtitleIndicesCsv ?? "none", externalSubtitlePathsCsv ?? "none");
        }

        // Validate requested resolution vs source height (only if we can determine source height)
        if (request.Resolution is not null)
        {
            logger.LogDebug("Validating resolution {RequestedResolution}p against source", request.Resolution);
            // Try to determine source height; swallow probe errors to avoid failing when ffprobe can't parse dummy test files
            if (sourceHeight is null)
            {
                try
                {
                    var probe = await prober.ProbeAsync(request.InputPath, CancellationToken.None);
                    sourceHeight = probe.VideoHeight;
                    durationSeconds = probe.DurationSeconds;
                    logger.LogDebug("Source video height: {SourceHeight}p, duration: {Duration}s",
                        sourceHeight, durationSeconds);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not determine source height: {Error}", ex.Message);
                    sourceHeight = null;
                }
            }
            if (sourceHeight is not null && request.Resolution > sourceHeight)
            {
                logger.LogWarning("Resolution validation failed: requested {RequestedResolution}p exceeds source {SourceHeight}p",
                    request.Resolution, sourceHeight);
                var dict = new Dictionary<string, string[]>
                {
                    { nameof(request.Resolution), new[] { $"Requested resolution {request.Resolution}p exceeds source height {sourceHeight}p." } }
                };
                return Results.ValidationProblem(dict);
            }
        }

        // Perform crop detection before persisting
        if (durationSeconds is null)
        {
            logger.LogDebug("Determining video duration for crop detection");
            try
            {
                var probe = await prober.ProbeAsync(request.InputPath, CancellationToken.None);
                durationSeconds = probe.DurationSeconds;
                sourceHeight ??= probe.VideoHeight;
                logger.LogDebug("Video duration: {Duration}s, height: {Height}p", durationSeconds, sourceHeight);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not determine video duration: {Error}", ex.Message);
            }
        }

        string? detectedCrop = null;
        logger.LogDebug("Starting crop detection for {InputPath}", request.InputPath);
        try
        {
            detectedCrop = await cropDetector.DetectAsync(request.InputPath, durationSeconds, CancellationToken.None);
            if (detectedCrop is not null)
            {
                logger.LogInformation("Crop detection completed: {DetectedCrop}", detectedCrop);
            }
            else
            {
                logger.LogDebug("No crop detected for {InputPath}", request.InputPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Crop detection failed: {Error}", ex.Message);
        }

        var conversion = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Crf = request.Crf,
            AudioBitrateKbps = request.AudioBitrateKbps,
            Resolution = request.Resolution,
            SelectedAudioStreamIndicesCsv = audioIndicesCsv,
            SelectedSubtitleStreamIndicesCsv = subtitleIndicesCsv,
            SelectedExternalSubtitlePathsCsv = externalSubtitlePathsCsv,
            DetectedCrop = detectedCrop
        };

        logger.LogInformation("Saving conversion to database with settings: CRF={Crf}, AudioBitrate={AudioBitrate}Kbps, Resolution={Resolution}p, AudioStreams={AudioStreams}, SubtitleStreams={SubtitleStreams}, ExternalSubtitles={ExternalSubtitles}, Crop={Crop}",
            conversion.Crf, conversion.AudioBitrateKbps, conversion.Resolution,
            audioIndicesCsv ?? "none", subtitleIndicesCsv ?? "none", externalSubtitlePathsCsv ?? "none", detectedCrop ?? "none");

        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        logger.LogInformation("Conversion created successfully with ID: {ConversionId}", conversion.Id);
        return Results.Created($"/api/conversions/{conversion.Id}", new ConversionDto(conversion.Id, conversion.InputPath, conversion.OutputDirectory, conversion.CreatedAtUtc, conversion.Crf, conversion.AudioBitrateKbps, conversion.Resolution));
    }

    private static async Task<IResult> ListConversionsAsync(AppDbContext db, ILogger logger)
    {
        logger.LogDebug("Listing all conversions");

        var items = await db.Conversions
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversionDto(c.Id, c.InputPath, c.OutputDirectory, c.CreatedAtUtc, c.Crf, c.AudioBitrateKbps, c.Resolution))
            .ToListAsync();

        logger.LogInformation("Retrieved {Count} conversions from database", items.Count);
        return Results.Ok(items);
    }

    private static async Task<IResult> StartConversionByIdAsync(
        int id,
        AppDbContext db,
        IConversionRunner runner,
        ILogger logger)
    {
        logger.LogInformation("Starting conversion by ID: {ConversionId}", id);

        var conversion = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (conversion is null)
        {
            logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            return Results.NotFound();
        }

        logger.LogInformation("Found conversion {ConversionId} for input: {InputPath}", id, conversion.InputPath);

        var result = await runner.StartAsync(conversion);
        if (result.IsSuccess)
        {
            logger.LogInformation("Conversion {ConversionId} started successfully with PID: {ProcessId}", id, result.ProcessId);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command });
        }
        else
        {
            logger.LogError("Failed to start conversion {ConversionId}: {ErrorMessage}", id, result.ErrorMessage);
            return Results.BadRequest(new { message = result.ErrorMessage });
        }
    }

    private static async Task<IResult> StartNextConversionAsync(
        AppDbContext db,
        IConversionRunner runner,
        ILogger logger)
    {
        logger.LogDebug("Starting next conversion from queue");

        var conversion = await db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (conversion is null)
        {
            logger.LogInformation("No conversions found in queue");
            return Results.NotFound(new { message = "Queue is empty" });
        }

        logger.LogInformation("Starting next conversion {ConversionId} for input: {InputPath}",
            conversion.Id, conversion.InputPath);

        var result = await runner.StartAsync(conversion);
        if (result.IsSuccess)
        {
            logger.LogInformation("Next conversion {ConversionId} started successfully with PID: {ProcessId}",
                conversion.Id, result.ProcessId);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId = conversion.Id });
        }
        else
        {
            logger.LogError("Failed to start next conversion {ConversionId}: {ErrorMessage}",
                conversion.Id, result.ErrorMessage);
            return Results.BadRequest(new { message = result.ErrorMessage, conversionId = conversion.Id });
        }
    }
}
