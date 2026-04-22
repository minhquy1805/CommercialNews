using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryAttempts;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;
using Notifications.Application.UseCases.EmailDeliveries.ProcessEmailDelivery;
using Notifications.Application.UseCases.EmailDeliveries.ProcessPendingEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;
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
        services.AddScoped<IProcessEmailDeliveryUseCase, ProcessEmailDeliveryUseCase>();
        services.AddScoped<IProcessPendingEmailDeliveriesUseCase, ProcessPendingEmailDeliveriesUseCase>();
        services.AddScoped<IProcessPendingOutboxMessagesUseCase, ProcessPendingOutboxMessagesUseCase>();
        services.AddScoped<IProcessOutboxMessageUseCase, ProcessOutboxMessageUseCase>();

        // Deferred for later phase:
        // - ProcessEmailDeliveryUseCase
        // - Outbox use cases
        // These will be revisited after:
        // 1. service contract models are stabilized
        // 2. runtime/internal processing flow is finalized
        // 3. shared outbox ownership is extracted from Notifications

        return services;
    }
}