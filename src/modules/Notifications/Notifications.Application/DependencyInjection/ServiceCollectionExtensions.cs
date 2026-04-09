using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;
using Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;
using Notifications.Application.UseCases.Outbox.GetOutboxMessageById;
using Notifications.Application.UseCases.Outbox.GetOutboxMessageByMessageId;
using Notifications.Application.UseCases.Outbox.MarkOutboxDeadLetter;
using Notifications.Application.UseCases.Outbox.MarkOutboxFailed;
using Notifications.Application.UseCases.Outbox.MarkOutboxPublished;
using Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;
using Notifications.Application.UseCases.Processing.ProcessEmailDelivery;

namespace Notifications.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IGetEmailDeliveriesUseCase, GetEmailDeliveriesUseCase>();
        services.AddScoped<IGetEmailDeliveryByIdUseCase, GetEmailDeliveryByIdUseCase>();
        services.AddScoped<IGetEmailDeliveryByMessageIdUseCase, GetEmailDeliveryByMessageIdUseCase>();
        services.AddScoped<IRetryEmailDeliveryUseCase, RetryEmailDeliveryUseCase>();

        services.AddScoped<IProcessEmailDeliveryUseCase, ProcessEmailDeliveryUseCase>();

        services.AddScoped<IGetOutboxMessageByIdUseCase, GetOutboxMessageByIdUseCase>();
        services.AddScoped<IGetOutboxMessageByMessageIdUseCase, GetOutboxMessageByMessageIdUseCase>();
        services.AddScoped<IProcessOutboxMessageUseCase, ProcessOutboxMessageUseCase>();
        services.AddScoped<IMarkOutboxPublishedUseCase, MarkOutboxPublishedUseCase>();
        services.AddScoped<IMarkOutboxFailedUseCase, MarkOutboxFailedUseCase>();
        services.AddScoped<IMarkOutboxDeadLetterUseCase, MarkOutboxDeadLetterUseCase>();

        return services;
    }
}