using VideoConverter.Api.Features.Profiles.Api;
using VideoConverter.Api.Features.Profiles.Validation;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api.Features.Profiles.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProfilesFeature(this IServiceCollection services)
    {
        _ = services.AddScoped<IRequestValidator<ProfileCreateDto>, ProfileCreateValidator>();
        _ = services.AddScoped<IRequestValidator<ProfilePatchDto>, ProfilePatchValidator>();
        _ = services.AddScoped<ProfileExistsValidator>();
        _ = services.AddScoped<ProfilesEndpoints>();
        _ = services.AddScoped<ProfileConversionsEndpoints>();
        return services;
    }
}
