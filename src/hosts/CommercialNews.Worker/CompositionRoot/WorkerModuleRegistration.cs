using Authorization.Application.DependencyInjection;
using Authorization.Infrastructure.DependencyInjection;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.Worker.Authorization;
using CommercialNews.Worker.Configuration;
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

        services.AddAuthorizationApplication();
        services.AddAuthorizationInfrastructure(configuration);

        services.AddOptions<OutboxWorkerOptions>(OutboxWorkerOptionNames.AuthorizationOutbox)
            .Bind(configuration.GetSection("Workers:AuthorizationOutbox"));

        services.Configure<NotificationWorkerOptions>(
            configuration.GetSection("Workers:Notifications"));

        services.AddHostedService<NotificationPollingService>();
        services.AddHostedService<OutboxPollingService>();
        services.AddHostedService<AuthorizationOutboxPollingService>();

        return services;
    }
}