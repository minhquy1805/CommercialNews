using CommercialNews.BuildingBlocks.Outbox.Exceptions;
using CommercialNews.BuildingBlocks.Outbox.Persistence;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Options;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommercialNews.BuildingBlocks.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SqlOptions>(configuration.GetSection("Sql"));

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPublicIdGenerator, UlidPublicIdGenerator>();

        services.AddSingleton<OutboxSqlExceptionTranslator>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();

        return services;
    }
}