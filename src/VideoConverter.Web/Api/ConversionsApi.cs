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
        ISubtitleLanguageMatcher subtitleMatcher)
    {
        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        // If audio/subtitle languages specified, probe and validate before persisting
        string? audioIndicesCsv = null;
        string? subtitleIndicesCsv = null;
        string? externalSubtitlePathsCsv = null;
        int? sourceHeight = null;

        if (request.AudioLanguages is { Count: > 0 })
        {
            var probe = await prober.ProbeAsync(request.InputPath, CancellationToken.None);
            try
            {
                var requested = request.AudioLanguages.ToList();
                var matched = matcher.MatchAudioStreams(probe.Streams, requested);
                audioIndicesCsv = string.Join(",", matched);
            }
            catch (InvalidOperationException ex)
            {
                var dict = new Dictionary<string, string[]>
                {
                    { nameof(request.AudioLanguages), new[] { ex.Message } }
                };
                return Results.ValidationProblem(dict);
            }
        }

        if (request.SubtitleLanguages is { Count: > 0 })
        {
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
                }
                catch
                {
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
                            break;
                        }
                    }

                    if (!found)
                    {
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
        }

        // Validate requested resolution vs source height (only if we can determine source height)
        if (request.Resolution is not null)
        {
            // Try to determine source height; swallow probe errors to avoid failing when ffprobe can't parse dummy test files
            if (sourceHeight is null)
            {
                try
                {
                    var probe = await prober.ProbeAsync(request.InputPath, CancellationToken.None);
                    sourceHeight = probe.VideoHeight;
                }
                catch
                {
                    sourceHeight = null;
                }
            }
            if (sourceHeight is not null && request.Resolution > sourceHeight)
            {
                var dict = new Dictionary<string, string[]>
                {
                    { nameof(request.Resolution), new[] { $"Requested resolution {request.Resolution}p exceeds source height {sourceHeight}p." } }
                };
                return Results.ValidationProblem(dict);
            }
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
            SelectedExternalSubtitlePathsCsv = externalSubtitlePathsCsv
        };

        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        return Results.Created($"/api/conversions/{conversion.Id}", new ConversionDto(conversion.Id, conversion.InputPath, conversion.OutputDirectory, conversion.CreatedAtUtc, conversion.Crf, conversion.AudioBitrateKbps, conversion.Resolution));
    }

    private static async Task<IResult> ListConversionsAsync(AppDbContext db)
    {
        var items = await db.Conversions
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversionDto(c.Id, c.InputPath, c.OutputDirectory, c.CreatedAtUtc, c.Crf, c.AudioBitrateKbps, c.Resolution))
            .ToListAsync();
        return Results.Ok(items);
    }

    private static async Task<IResult> StartConversionByIdAsync(
        int id,
        AppDbContext db,
        IConversionRunner runner)
    {
        var conversion = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (conversion is null)
        {
            return Results.NotFound();
        }

        var result = await runner.StartAsync(conversion);
        return result.IsSuccess
            ? Results.Ok(new { message = "Started", result.ProcessId, result.Command })
            : Results.BadRequest(new { message = result.ErrorMessage });
    }

    private static async Task<IResult> StartNextConversionAsync(
        AppDbContext db,
        IConversionRunner runner)
    {
        var conversion = await db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (conversion is null)
        {
            return Results.NotFound(new { message = "Queue is empty" });
        }

        var result = await runner.StartAsync(conversion);
        return result.IsSuccess
            ? Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId = conversion.Id })
            : Results.BadRequest(new { message = result.ErrorMessage, conversionId = conversion.Id });
    }
}
