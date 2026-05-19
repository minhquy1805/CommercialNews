using Media.Application.Ports.Persistence;
using Media.Application.Ports.Services;
using Media.Infrastructure.Persistence.Exceptions;
using Media.Infrastructure.Persistence.Repositories;
using Media.Infrastructure.Persistence.Sql;
using Media.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Media.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<MediaUnitOfWork>();
            services.AddScoped<IMediaUnitOfWork>(sp => sp.GetRequiredService<MediaUnitOfWork>());

            services.AddSingleton<MediaSqlExceptionTranslator>();

            services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
            services.AddScoped<IArticleMediaRepository, ArticleMediaRepository>();
            services.AddScoped<IArticleMediaSetRepository, ArticleMediaSetRepository>();
            services.AddScoped<IMediaOutboxWriter, MediaOutboxWriter>();

            return services;
        }
    }
}