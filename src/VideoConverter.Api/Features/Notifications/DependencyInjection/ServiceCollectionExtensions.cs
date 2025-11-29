using VideoConverter.Api.Features.Notifications.Services;

namespace VideoConverter.Api.Features.Notifications.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        _ = services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}
