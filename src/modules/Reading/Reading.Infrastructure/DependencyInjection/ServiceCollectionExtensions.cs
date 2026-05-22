using Microsoft.Extensions.DependencyInjection;
using Reading.Application.Ports.Persistence;
using Reading.Application.Ports.Seo;
using Reading.Infrastructure.Persistence.Exceptions;
using Reading.Infrastructure.Persistence.Repositories;
using Reading.Infrastructure.Persistence.Sql;
using Reading.Infrastructure.Seo;

namespace Reading.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadingInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ReadingSqlExceptionTranslator>();

        services.AddScoped<ReadingUnitOfWork>();
        services.AddScoped<IReadingUnitOfWork>(
            serviceProvider => serviceProvider.GetRequiredService<ReadingUnitOfWork>());

        services.AddScoped<IArticleReadModelRepository, ArticleReadModelRepository>();

        services.AddScoped<ISeoRouteResolver, SeoRouteResolver>();

        return services;
    }
}