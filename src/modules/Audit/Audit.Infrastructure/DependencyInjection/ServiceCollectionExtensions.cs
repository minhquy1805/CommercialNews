using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Normalization;
using Audit.Application.Abstractions.Serialization;
using Audit.Domain.Policies.Redaction;
using Audit.Infrastructure.Normalization.Authorization;
using Audit.Infrastructure.Normalization.Content;
using Audit.Infrastructure.Normalization.Identity;
using Audit.Infrastructure.Normalization.Interaction;
using Audit.Infrastructure.Normalization.Media;
using Audit.Infrastructure.Persistence;
using Audit.Infrastructure.Persistence.Exceptions;
using Audit.Infrastructure.Persistence.Repositories;
using Audit.Infrastructure.Redaction;
using Audit.Infrastructure.Serialization;
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

        services.AddSingleton<IAuditJsonSerializer, AuditJsonSerializer>();
        services.AddSingleton<IAuditRedactionPolicy, DefaultAuditRedactionPolicy>();
        services.AddSingleton<IAuditEventNormalizer, AuthorizationAuditEventNormalizer>();
        services.AddSingleton<IAuditEventNormalizer, IdentityAuditEventNormalizer>();
        services.AddSingleton<IAuditEventNormalizer, ContentAuditEventNormalizer>();
        services.AddSingleton<IAuditEventNormalizer, MediaAuditEventNormalizer>();
        services.AddSingleton<IAuditEventNormalizer, InteractionAuditEventNormalizer>();

        return services;
    }
}
