using Microsoft.Extensions.DependencyInjection;
using Seo.Application.Ports.Persistence;
using Seo.Infrastructure.Persistence.Exceptions;
using Seo.Infrastructure.Persistence.Repositories;
using Seo.Infrastructure.Persistence.Sql;

namespace Seo.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSeoInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<SeoUnitOfWork>();
        services.AddScoped<ISeoUnitOfWork>(sp => sp.GetRequiredService<SeoUnitOfWork>());

        services.AddSingleton<SeoSqlExceptionTranslator>();

        services.AddScoped<ISeoMetadataRepository, SeoMetadataRepository>();
        services.AddScoped<ISlugRegistryRepository, SlugRegistryRepository>();

        return services;
    }
}