using Authorization.Application.DependencyInjection;
using Authorization.Infrastructure.DependencyInjection;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.Worker.Configuration;
using CommercialNews.Worker.Outbox;
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

        services.AddOptions<OutboxWorkerOptions>()
            .Bind(configuration.GetSection("Workers:Outbox"));

        services.AddOptions<OutboxWorkerOptions>()
            .Bind(configuration.GetSection("Workers:Outbox"));

        services.Configure<OutboxProcessingOptions>(
            configuration.GetSection("Workers:Outbox:Processing"));

        services.AddHostedService<OutboxPollingService>();


        return services;
    }
}