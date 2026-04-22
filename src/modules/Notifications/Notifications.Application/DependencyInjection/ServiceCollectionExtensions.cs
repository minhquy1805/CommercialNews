using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryAttempts;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;
using Notifications.Application.UseCases.EmailDeliveries.ProcessEmailDelivery;
using Notifications.Application.UseCases.EmailDeliveries.ProcessPendingEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;
using Notifications.Application.UseCases.Outbox.GetOutboxMessageById;
using Notifications.Application.UseCases.Outbox.GetOutboxMessageByMessageId;
using Notifications.Application.UseCases.Outbox.MarkOutboxDead;
using Notifications.Application.UseCases.Outbox.MarkOutboxFailed;
using Notifications.Application.UseCases.Outbox.MarkOutboxPublished;
using Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;
using Notifications.Application.UseCases.Outbox.ProcessPendingOutboxMessages;

namespace Notifications.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Email delivery read / admin operational use cases
        services.AddScoped<IGetEmailDeliveriesUseCase, GetEmailDeliveriesUseCase>();
        services.AddScoped<IGetEmailDeliveryByIdUseCase, GetEmailDeliveryByIdUseCase>();
        services.AddScoped<IGetEmailDeliveryByMessageIdUseCase, GetEmailDeliveryByMessageIdUseCase>();
        services.AddScoped<IGetEmailDeliveryAttemptsUseCase, GetEmailDeliveryAttemptsUseCase>();
        services.AddScoped<IRetryEmailDeliveryUseCase, RetryEmailDeliveryUseCase>();

        // Email delivery runtime use cases
        services.AddScoped<IProcessEmailDeliveryUseCase, ProcessEmailDeliveryUseCase>();
        services.AddScoped<IProcessPendingEmailDeliveriesUseCase, ProcessPendingEmailDeliveriesUseCase>();

        // Outbox runtime use cases
        services.AddScoped<IProcessPendingOutboxMessagesUseCase, ProcessPendingOutboxMessagesUseCase>();
        services.AddScoped<IProcessOutboxMessageUseCase, ProcessOutboxMessageUseCase>();

        // Outbox admin / operational use cases
        services.AddScoped<IGetOutboxMessageByIdUseCase, GetOutboxMessageByIdUseCase>();
        services.AddScoped<IGetOutboxMessageByMessageIdUseCase, GetOutboxMessageByMessageIdUseCase>();
        services.AddScoped<IMarkOutboxPublishedUseCase, MarkOutboxPublishedUseCase>();
        services.AddScoped<IMarkOutboxFailedUseCase, MarkOutboxFailedUseCase>();
        services.AddScoped<IMarkOutboxDeadUseCase, MarkOutboxDeadUseCase>();

        return services;
    }
}