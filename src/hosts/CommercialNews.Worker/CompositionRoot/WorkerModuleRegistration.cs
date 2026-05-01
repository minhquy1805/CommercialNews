using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.Worker.Configuration;
using CommercialNews.Worker.Notifications.Consumers;
using CommercialNews.Worker.Notifications.Handlers;
using CommercialNews.Worker.Notifications.Handlers.Identity;
using CommercialNews.Worker.Notifications.Processing;
using CommercialNews.Worker.Outbox;
using CommercialNews.Worker.Outbox.Handlers.Authorization;
using CommercialNews.Worker.Outbox.Handlers.Identity;
using CommercialNews.Worker.Outbox.Handlers.Notifications;
using CommercialNews.Worker.Outbox.Publishing;
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

        services.AddNotificationsApplication(configuration);
        services.AddNotificationsInfrastructure(configuration);

        services.AddOptions<OutboxWorkerOptions>()
            .Bind(configuration.GetSection("Workers:Outbox"));

        services.Configure<OutboxProcessingOptions>(
            configuration.GetSection("Workers:Outbox:Processing"));

        services.Configure<OutboxRabbitMqOptions>(
            configuration.GetSection(OutboxRabbitMqOptions.SectionName));

        services.AddScoped<IOutboxEventPublisher, RabbitMqOutboxEventPublisher>();

        services.AddScoped<IOutboxMessageHandler, IdentityVerificationEmailRequestedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityPasswordResetRequestedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityPasswordChangedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityEmailVerifiedOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, NotificationsEmailSentOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, NotificationsEmailFailedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, NotificationsEmailDeadOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, AuthorizationUserRoleAssignedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationUserRoleRevokedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationRolePermissionGrantedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationRolePermissionRevokedOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, AuthorizationRoleCreatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationRoleUpdatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationRoleActivatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationRoleDeactivatedOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, AuthorizationPermissionCreatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationPermissionUpdatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationPermissionActivatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, AuthorizationPermissionDeactivatedOutboxHandler>();

        services.Configure<NotificationsRabbitMqConsumerOptions>(
            configuration.GetSection(NotificationsRabbitMqConsumerOptions.SectionName));

        services.AddScoped<NotificationsIntegrationEventDispatcher>();

        services.AddScoped<INotificationsIntegrationEventHandler, IdentityVerificationEmailRequestedIntegrationEventHandler>();
        services.AddScoped<INotificationsIntegrationEventHandler, IdentityPasswordResetRequestedIntegrationEventHandler>();
        services.AddScoped<INotificationsIntegrationEventHandler, IdentityPasswordChangedIntegrationEventHandler>();
        services.AddScoped<INotificationsIntegrationEventHandler, IdentityEmailVerifiedIntegrationEventHandler>();

        services.Configure<EmailDeliveryProcessingWorkerOptions>(
            configuration.GetSection(EmailDeliveryProcessingWorkerOptions.SectionName));

        services.AddHostedService<OutboxPollingService>();
        services.AddHostedService<NotificationsRabbitMqConsumerService>();
        services.AddHostedService<EmailDeliveryProcessingWorker>();

        return services;
    }
}