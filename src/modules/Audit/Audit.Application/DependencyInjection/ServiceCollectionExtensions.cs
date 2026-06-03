using Audit.Application.Abstractions.Normalization;
using Audit.Application.Behaviors;
using Audit.Application.Services.Evidence;
using Audit.Application.Services.Ingestion;
using Audit.Application.Services.Mapping;
using Audit.Application.Services.Normalization;
using Audit.Application.Services.Redaction;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(ServiceCollectionExtensions).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(
            assembly,
            includeInternalTypes: true);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(AuditValidationBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(AuditTransactionBehavior<,>));

        // Normalization
        services.AddSingleton<
            IAuditEventNormalizerRegistry,
            AuditEventNormalizerRegistry>();

        // Services
        services.AddScoped<
            IAuditRedactionService,
            AuditRedactionService>();

        services.AddScoped<
            IAuditEvidenceBuilder,
            AuditEvidenceBuilder>();

        services.AddScoped<
            IAuditIngestionApplicationService,
            AuditIngestionApplicationService>();

        services.AddScoped<
            IAuditIngestionFailureClassifier,
            AuditIngestionFailureClassifier>();

        services.AddScoped<
            IAuditResultMapper,
            AuditResultMapper>();

        return services;
    }
}