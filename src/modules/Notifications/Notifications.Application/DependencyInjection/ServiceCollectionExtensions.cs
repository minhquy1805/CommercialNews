using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryAttempts;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;
using Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;
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

        return services;
    }
}