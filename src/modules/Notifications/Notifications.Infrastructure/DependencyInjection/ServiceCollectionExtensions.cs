using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Services;
using Notifications.Application.Ports.Transactions;
using Notifications.Infrastructure.Persistence.Exceptions;
using Notifications.Infrastructure.Persistence.Repositories;
using Notifications.Infrastructure.Persistence.Sql;
using Notifications.Infrastructure.Services;

namespace Notifications.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<NotificationEmailOptions>(
            configuration.GetSection("Notifications:Email"));

        services.AddScoped<NotificationsUnitOfWork>();
        services.AddScoped<INotificationsUnitOfWork>(
            sp => sp.GetRequiredService<NotificationsUnitOfWork>());

        services.AddSingleton<NotificationsSqlExceptionTranslator>();

        services.AddScoped<IEmailDeliveryRepository, EmailDeliveryRepository>();
        services.AddScoped<IEmailDeliveryAttemptRepository, EmailDeliveryAttemptRepository>();
        services.AddScoped<IEmailDeliveryQueryRepository, EmailDeliveryQueryRepository>();

        services.AddScoped<INotificationRetryPolicy, NotificationRetryPolicy>();
        services.AddScoped<IProviderResultClassifier, ProviderResultClassifier>();
        services.AddScoped<INotificationTemplateRenderer, NotificationTemplateRenderer>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<INotificationDedupeService, NotificationDedupeService>();

        return services;
    }
}