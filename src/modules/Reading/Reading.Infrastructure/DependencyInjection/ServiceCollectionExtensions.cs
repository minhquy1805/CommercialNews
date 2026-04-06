using Microsoft.Extensions.DependencyInjection;
using Reading.Application.Ports.Persistence;
using Reading.Infrastructure.Persistence.Repositories;

namespace Reading.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadingInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IReadingQueryRepository, ReadingQueryRepository>();

        return services;
    }
}