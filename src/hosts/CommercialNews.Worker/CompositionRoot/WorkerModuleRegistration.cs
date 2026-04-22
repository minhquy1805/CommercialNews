using CommercialNews.Worker.Notifications;
using Notifications.Application.DependencyInjection;
using Notifications.Infrastructure.DependencyInjection;

namespace CommercialNews.Worker.CompositionRoot;

public static class WorkerModuleRegistration
{
    public static IServiceCollection AddWorkerModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddNotificationsApplication();
        services.AddNotificationsInfrastructure(configuration);

        services.Configure<NotificationWorkerOptions>(
            configuration.GetSection("Workers:Notifications"));

        services.AddHostedService<NotificationPollingService>();
        services.AddHostedService<OutboxPollingService>();

        return services;
    }
}