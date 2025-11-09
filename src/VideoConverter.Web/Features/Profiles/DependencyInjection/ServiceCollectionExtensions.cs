using VideoConverter.Web.Features.Profiles.Api;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Profiles.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProfilesFeature(this IServiceCollection services)
    {
        _ = services.AddScoped<IRequestValidator<Dtos.ProfileCreateDto>, ProfileCreateValidator>();
        _ = services.AddScoped<IRequestValidator<Dtos.ProfilePatchDto>, ProfilePatchValidator>();
        _ = services.AddScoped<ProfileExistsValidator>();
        _ = services.AddScoped<ProfilesEndpoints>();
        _ = services.AddScoped<ProfileConversionsEndpoints>();
        return services;
    }
}
