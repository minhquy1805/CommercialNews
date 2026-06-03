using Audit.Application.Abstractions.Persistence;
using Audit.Infrastructure.Persistence;
using Audit.Infrastructure.Persistence.Exceptions;
using Audit.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<AuditUnitOfWork>();
        services.AddScoped<IAuditUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<AuditUnitOfWork>());

        services.AddScoped<AuditSqlExceptionTranslator>();

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditIngestionRepository, AuditIngestionRepository>();

        return services;
    }
}
