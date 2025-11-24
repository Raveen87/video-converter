namespace VideoConverter.Api.Features.FileBrowser.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileBrowserFeature(this IServiceCollection services)
    {
        _ = services.AddScoped<IFileBrowserService, FileBrowserService>();
        return services;
    }
}
