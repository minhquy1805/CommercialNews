using Audit.Application.DependencyInjection;
using Audit.Infrastructure.DependencyInjection;
using Authorization.Application.Consumers.Identity;
using Authorization.Infrastructure.DependencyInjection;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.Worker.Authorization.Consumers;
using CommercialNews.Worker.Authorization.Handlers;
using CommercialNews.Worker.Authorization.Handlers.Identity;
using CommercialNews.Worker.Audit.Consumers;
using CommercialNews.Worker.Audit.Handlers;
using CommercialNews.Worker.Audit.Handlers.Authorization;
using CommercialNews.Worker.Audit.Handlers.Identity;
using CommercialNews.Worker.Configuration;
using CommercialNews.Worker.Interaction.Consumers;
using CommercialNews.Worker.Interaction.Handlers;
using CommercialNews.Worker.Interaction.Handlers.Content;
using CommercialNews.Worker.Notifications.Consumers;
using CommercialNews.Worker.Notifications.Handlers;
using CommercialNews.Worker.Notifications.Handlers.Identity;
using CommercialNews.Worker.Notifications.Processing;
using CommercialNews.Worker.Outbox;
using CommercialNews.Worker.Outbox.Handlers.Authorization;
using CommercialNews.Worker.Outbox.Handlers.Identity;
using CommercialNews.Worker.Outbox.Handlers.Interaction;
using CommercialNews.Worker.Outbox.Handlers.Notifications;
using CommercialNews.Worker.Outbox.Publishing;
using CommercialNews.Worker.Outbox.Handlers.Seo;
using Notifications.Application.DependencyInjection;
using Notifications.Infrastructure.DependencyInjection;
using CommercialNews.Worker.Outbox.Handlers.Content;
using CommercialNews.Worker.Audit.Handlers.Content;
using CommercialNews.Worker.Seo.Handlers;
using CommercialNews.Worker.Seo.Handlers.Content;
using CommercialNews.Worker.Seo.Consumers;
using Seo.Application.DependencyInjection;
using Seo.Infrastructure.DependencyInjection;
using CommercialNews.Worker.Outbox.Handlers.Media;
using CommercialNews.Worker.Audit.Handlers.Media;
using Reading.Application.DependencyInjection;
using Reading.Infrastructure.DependencyInjection;
using CommercialNews.Worker.Reading.Consumers;
using CommercialNews.Worker.Reading.Handlers;
using CommercialNews.Worker.Reading.Handlers.Content;
using CommercialNews.Worker.Reading.Handlers.Identity;
using CommercialNews.Worker.Reading.Handlers.Media;
using CommercialNews.Worker.Reading.Handlers.Seo;
using Interaction.Application.DependencyInjection;
using Interaction.Infrastructure.DependencyInjection;

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

        services.AddAuthorizationInfrastructure(configuration);
        services.AddScoped<
            IIdentityUserRegisteredConsumerService,
            IdentityUserRegisteredConsumerService>();

        services.AddAuditApplication();
        services.AddAuditInfrastructure();

        services.AddSeoConsumerApplication();
        services.AddSeoInfrastructure();

        services.AddReadingApplication();
        services.AddReadingInfrastructure();

        services.AddInteractionApplication();
        services.AddInteractionInfrastructure();

        services.AddOptions<OutboxWorkerOptions>()
            .Bind(configuration.GetSection("Workers:Outbox"));

        services.Configure<OutboxProcessingOptions>(
            configuration.GetSection("Workers:Outbox:Processing"));

        services.Configure<OutboxRabbitMqOptions>(
            configuration.GetSection(OutboxRabbitMqOptions.SectionName));

        services.AddScoped<IOutboxEventPublisher, RabbitMqOutboxEventPublisher>();

        services.AddScoped<IOutboxMessageHandler, IdentityUserRegisteredOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityUserPublicProfileUpdatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityVerificationEmailRequestedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityPasswordResetRequestedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityPasswordChangedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityEmailVerifiedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityUserActivatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityUserDisabledOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityUserLockedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityUserUnlockedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityEmailMarkedVerifiedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, IdentityUserSessionsRevokedOutboxHandler>();

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

        services.AddScoped<IOutboxMessageHandler, ContentArticleCreatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ContentArticleUpdatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ContentArticlePublishedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ContentArticleUnpublishedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ContentArticleArchivedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ContentArticleSoftDeletedOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, MediaAssetRegisteredOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, MediaAssetUpdatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, MediaAssetSoftDeletedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, MediaAssetRestoredOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, ArticleMediaAttachedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ArticleMediaDetachedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ArticleMediaReorderedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ArticlePrimaryMediaSetOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, SlugRouteChangedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, SlugRouteDeactivatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, SeoMetadataUpdatedOutboxHandler>();

        services.AddScoped<IOutboxMessageHandler, InteractionArticleLikedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionArticleUnlikedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionCommentCreatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionCommentHiddenOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionCommentRestoredOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionCommentDeletedByAuthorOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionCommentReportedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionCommentReportsDismissedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionCommentReportAlertTriggeredOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, InteractionArticleCountersProjectionPublishedOutboxHandler>();

        services.Configure<NotificationsRabbitMqConsumerOptions>(
            configuration.GetSection(NotificationsRabbitMqConsumerOptions.SectionName));

        services.AddScoped<NotificationsIntegrationEventDispatcher>();

        services.AddScoped<INotificationsIntegrationEventHandler, IdentityVerificationEmailRequestedIntegrationEventHandler>();
        services.AddScoped<INotificationsIntegrationEventHandler, IdentityPasswordResetRequestedIntegrationEventHandler>();
        services.AddScoped<INotificationsIntegrationEventHandler, IdentityPasswordChangedIntegrationEventHandler>();
        services.AddScoped<INotificationsIntegrationEventHandler, IdentityEmailVerifiedIntegrationEventHandler>();

        services.Configure<AuthorizationRabbitMqConsumerOptions>(
            configuration.GetSection(AuthorizationRabbitMqConsumerOptions.SectionName));

        services.AddScoped<AuthorizationIntegrationEventDispatcher>();

        services.AddScoped<IAuthorizationIntegrationEventHandler, IdentityUserRegisteredIntegrationEventHandler>();

        services.Configure<AuditRabbitMqConsumerOptions>(
            configuration.GetSection(AuditRabbitMqConsumerOptions.SectionName));

        services.Configure<SeoRabbitMqConsumerOptions>(
            configuration.GetSection(SeoRabbitMqConsumerOptions.SectionName));

        services.Configure<ReadingRabbitMqConsumerOptions>(
            configuration.GetSection(ReadingRabbitMqConsumerOptions.SectionName));

        services.Configure<InteractionRabbitMqConsumerOptions>(
            configuration.GetSection(InteractionRabbitMqConsumerOptions.SectionName));

        services.AddScoped<AuditIntegrationEventDispatcher>();

        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationUserRoleAssignedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationUserRoleRevokedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationRolePermissionGrantedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationRolePermissionRevokedAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationRoleCreatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationRoleUpdatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationRoleActivatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationRoleDeactivatedAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationPermissionCreatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationPermissionUpdatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationPermissionActivatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, AuthorizationPermissionDeactivatedAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, IdentityEmailVerifiedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, IdentityPasswordChangedAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, IdentityUserActivatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, IdentityUserDisabledAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, IdentityUserLockedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, IdentityUserUnlockedAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, IdentityEmailMarkedVerifiedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, IdentityUserSessionsRevokedAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, ContentArticleCreatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ContentArticleUpdatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ContentArticlePublishedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ContentArticleUnpublishedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ContentArticleArchivedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ContentArticleSoftDeletedAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, MediaAssetRegisteredAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, MediaAssetUpdatedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, MediaAssetSoftDeletedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, MediaAssetRestoredAuditHandler>();

        services.AddScoped<IAuditIntegrationEventHandler, ArticleMediaAttachedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ArticleMediaDetachedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ArticleMediaReorderedAuditHandler>();
        services.AddScoped<IAuditIntegrationEventHandler, ArticlePrimaryMediaSetAuditHandler>();

        services.AddScoped<SeoIntegrationEventDispatcher>();

        services.AddScoped<ISeoIntegrationEventHandler, ContentArticleCreatedSeoHandler>();
        services.AddScoped<ISeoIntegrationEventHandler, ContentArticleUpdatedSeoHandler>();
        services.AddScoped<ISeoIntegrationEventHandler, ContentArticlePublishedSeoHandler>();
        services.AddScoped<ISeoIntegrationEventHandler, ContentArticleUnpublishedSeoHandler>();
        services.AddScoped<ISeoIntegrationEventHandler, ContentArticleArchivedSeoHandler>();
        services.AddScoped<ISeoIntegrationEventHandler, ContentArticleSoftDeletedSeoHandler>();

        services.AddScoped<ReadingIntegrationEventDispatcher>();

        services.AddScoped<IReadingIntegrationEventHandler, ContentArticlePublishedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, ContentArticleUpdatedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, ContentArticleUnpublishedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, ContentArticleArchivedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, ContentArticleSoftDeletedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, MediaArticleMediaAttachedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, MediaArticlePrimaryMediaSetReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, MediaArticleMediaReorderedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, MediaArticleMediaDetachedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, SlugRouteChangedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, SlugRouteDeactivatedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, SeoMetadataUpdatedReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, IdentityUserRegisteredReadingHandler>();
        services.AddScoped<IReadingIntegrationEventHandler, IdentityUserPublicProfileUpdatedReadingHandler>();

        services.AddScoped<InteractionIntegrationEventDispatcher>();

        services.AddScoped<IInteractionIntegrationEventHandler, ContentArticlePublishedInteractionHandler>();
        services.AddScoped<IInteractionIntegrationEventHandler, ContentArticleUnpublishedInteractionHandler>();
        services.AddScoped<IInteractionIntegrationEventHandler, ContentArticleArchivedInteractionHandler>();
        services.AddScoped<IInteractionIntegrationEventHandler, ContentArticleSoftDeletedInteractionHandler>();

        services.Configure<EmailDeliveryProcessingWorkerOptions>(
            configuration.GetSection(EmailDeliveryProcessingWorkerOptions.SectionName));

        services.AddHostedService<OutboxPollingService>();
        services.AddHostedService<AuthorizationRabbitMqConsumerService>();
        services.AddHostedService<NotificationsRabbitMqConsumerService>();
        services.AddHostedService<AuditRabbitMqConsumerService>();
        services.AddHostedService<SeoRabbitMqConsumerService>();
        services.AddHostedService<ReadingRabbitMqConsumerService>();
        services.AddHostedService<InteractionRabbitMqConsumerService>();
        services.AddHostedService<EmailDeliveryProcessingWorker>();

        return services;
    }
}
