using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Ports.Persistence.Read;
using Notifications.Application.Ports.Persistence.Transactions;
using Notifications.Application.Ports.Persistence.Write;
using Notifications.Infrastructure.Persistence.Exceptions;
using Notifications.Infrastructure.Persistence.Repositories.Read;
using Notifications.Infrastructure.Persistence.Repositories.Write;
using Notifications.Infrastructure.Persistence.Sql;

namespace Notifications.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<NotificationsUnitOfWork>();
        services.AddScoped<INotificationsUnitOfWork>(sp => sp.GetRequiredService<NotificationsUnitOfWork>());

        services.AddSingleton<NotificationsSqlExceptionTranslator>();

        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IEmailDeliveryRepository, EmailDeliveryRepository>();
        services.AddScoped<IEmailDeliveryAttemptRepository, EmailDeliveryAttemptRepository>();

        services.AddScoped<IOutboxMessageQueryRepository, OutboxMessageQueryRepository>();
        services.AddScoped<IEmailDeliveryQueryRepository, EmailDeliveryQueryRepository>();

        return services;
    }
}