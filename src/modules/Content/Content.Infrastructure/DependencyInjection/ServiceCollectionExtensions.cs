using Content.Application.Ports.Persistence;
using Content.Infrastructure.Persistence.Exceptions;
using Content.Infrastructure.Persistence.Repositories;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Content.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddContentInfrastructure(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<ContentUnitOfWork>();
            services.AddScoped<IContentUnitOfWork>(sp => sp.GetRequiredService<ContentUnitOfWork>());

            services.AddScoped<IArticleRepository, ArticleRepository>();
            services.AddScoped<IArticleLifecycleEventRepository, ArticleLifecycleEventRepository>();

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IArticleRevisionRepository, ArticleRevisionRepository>();
            services.AddScoped<ContentSqlExceptionTranslator>();

            return services;
        }
    }
}
