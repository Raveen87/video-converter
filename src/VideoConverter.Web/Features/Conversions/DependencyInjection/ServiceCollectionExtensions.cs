using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Conversions.Validation;
using VideoConverter.Web.Services.Conversion;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Conversions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConversionsFeature(this IServiceCollection services)
    {
        _ = services.AddSingleton<IConversionOutputPlanner, ConversionOutputPlanner>();
        _ = services.AddSingleton<IAudioLanguageMatcher, AudioLanguageMatcher>();
        _ = services.AddSingleton<ISubtitleLanguageMatcher, SubtitleLanguageMatcher>();
        _ = services.AddSingleton<EffectiveConversionRequestBuilder>();
        _ = services.AddScoped<IConversionService, ConversionService>();
        _ = services.AddScoped<IDirectoryQueueService, DirectoryQueueService>();
        _ = services.AddScoped<IConversionExecutionService, ConversionExecutionService>();
        _ = services.AddScoped<IRequestValidator<Dtos.ConversionCreateDto>, ConversionCreateValidator>();
        return services;
    }
}
