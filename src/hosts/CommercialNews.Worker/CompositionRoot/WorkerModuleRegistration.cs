using CommercialNews.BuildingBlocks.Time;
using Notifications.Application.DependencyInjection;
using Notifications.Infrastructure.DependencyInjection;
using CommercialNews.Worker.Messaging.Outbox.Notifications;
using CommercialNews.BuildingBlocks.Abstractions.Time;

namespace CommercialNews.Worker.CompositionRoot;

public static class WorkerModuleRegistration
{
    public static IServiceCollection AddWorkerModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddNotificationsApplication();
        services.AddNotificationsInfrastructure(configuration);

        services.Configure<NotificationWorkerOptions>(
            configuration.GetSection("Workers:Notifications"));

        services.AddHostedService<NotificationOutboxPollingService>();

        return services;
    }
}