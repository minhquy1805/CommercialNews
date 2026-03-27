using CommercialNews.BuildingBlocks.Persistence.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommercialNews.BuildingBlocks.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksSql(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SqlOptions>(configuration.GetSection("Sql"));
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        return services;
    }
}