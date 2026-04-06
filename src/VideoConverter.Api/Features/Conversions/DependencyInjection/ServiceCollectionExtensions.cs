using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;

namespace VideoConverter.Api.Features.Conversions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConversionsFeature(this IServiceCollection services)
    {
        _ = services.AddSingleton<IConversionOutputPlanner, ConversionOutputPlanner>();
        _ = services.AddSingleton<IAudioLanguageMatcher, AudioLanguageMatcher>();
        _ = services.AddSingleton<ISubtitleLanguageMatcher, SubtitleLanguageMatcher>();
        _ = services.AddSingleton<IMediaDurationProvider, MediaDurationProvider>();
        _ = services.AddSingleton<EffectiveConversionRequestBuilder>();
        _ = services.AddScoped<IConversionService, ConversionService>();
        _ = services.AddScoped<IDirectoryQueueService, DirectoryQueueService>();
        _ = services.AddScoped<IDirectoryBatchService, DirectoryBatchService>();
        _ = services.AddScoped<IConversionExecutionService, ConversionExecutionService>();
        _ = services.AddScoped<IConversionCleanupService, ConversionCleanupService>();
        _ = services.AddScoped<IDirectoryBatchCleanupService, DirectoryBatchCleanupService>();
        _ = services.AddScoped<ICrfOptimizationService, CrfOptimizationService>();
        _ = services.AddHostedService<ConversionCleanupWorker>();
        _ = services.AddHostedService<DirectoryBatchWorker>();
        _ = services.AddScoped<IRequestValidator<ConversionCreateDto>, ConversionCreateValidator>();
        _ = services.AddScoped<IRequestValidator<ConversionPatchDto>, ConversionPatchValidator>();
        _ = services.AddScoped<IRequestValidator<DirectoryQueueRequestDto>, DirectoryQueueRequestValidator>();
        _ = services.AddScoped<IRequestValidator<ProfileDirectoryQueueRequestDto>, ProfileDirectoryQueueRequestValidator>();
        _ = services.AddScoped<ConversionExistsValidator>();
        _ = services.AddScoped<ConversionsWriteApi>();
        return services;
    }
}
