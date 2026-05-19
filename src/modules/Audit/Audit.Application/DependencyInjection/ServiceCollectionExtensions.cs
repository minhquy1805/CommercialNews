using Audit.Application.Consumers.Authorization;
using Audit.Application.Consumers.Content;
using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Media;
using Audit.Application.Services;
using Audit.Application.UseCases.GetAuditLogByEventId;
using Audit.Application.UseCases.GetAuditLogById;
using Audit.Application.UseCases.GetAuditLogs;
using Audit.Application.UseCases.GetAuditLogsByCorrelationId;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Query use cases
        services.AddScoped<IGetAuditLogsUseCase, GetAuditLogsUseCase>();
        services.AddScoped<IGetAuditLogByIdUseCase, GetAuditLogByIdUseCase>();
        services.AddScoped<IGetAuditLogsByCorrelationIdUseCase, GetAuditLogsByCorrelationIdUseCase>();
        services.AddScoped<IGetAuditLogByEventIdUseCase, GetAuditLogByEventIdUseCase>();

        // Generic audit ingestion
        services.AddScoped<IAuditIngestionService, AuditIngestionService>();

        // Producer-specific audit ingestion mappers
        services.AddScoped<
            IAuthorizationAuditEventIngestionService,
            AuthorizationAuditEventIngestionService>();

        services.AddScoped<
            IIdentityAuditEventIngestionService,
            IdentityAuditEventIngestionService>();

        services.AddScoped<
            IContentAuditEventIngestionService,
            ContentAuditEventIngestionService>();

        services.AddScoped<
            IMediaAuditEventIngestionService,
            MediaAuditEventIngestionService>();

        return services;
    }
}