using CommercialNews.Worker.Messaging.Outbox.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddHostedService<NotificationOutboxPollingService>();

        return services;
    }
}