using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Configuration;
using Notifications.Application.Consumers.Identity;
using Notifications.Application.Services;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryAttempts;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;
using Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;

namespace Notifications.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<EmailDeliveryOptions>(
            configuration.GetSection(EmailDeliveryOptions.SectionName));

        // Email delivery read / admin operational use cases
        services.AddScoped<IGetEmailDeliveriesUseCase, GetEmailDeliveriesUseCase>();
        services.AddScoped<IGetEmailDeliveryByIdUseCase, GetEmailDeliveryByIdUseCase>();
        services.AddScoped<IGetEmailDeliveryByMessageIdUseCase, GetEmailDeliveryByMessageIdUseCase>();
        services.AddScoped<IGetEmailDeliveryAttemptsUseCase, GetEmailDeliveryAttemptsUseCase>();
        services.AddScoped<IRetryEmailDeliveryUseCase, RetryEmailDeliveryUseCase>();

        // Notifications consumers / ingestion workflows
        services.AddScoped<IIdentityEmailEventIngestionService, IdentityEmailEventIngestionService>();

        // Email delivery processing workflow
        services.AddScoped<IEmailDeliveryProcessingService, EmailDeliveryProcessingService>();

        return services;
    }
}