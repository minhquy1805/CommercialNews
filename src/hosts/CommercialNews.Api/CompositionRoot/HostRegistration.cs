using CommercialNews.Api.Health;
using CommercialNews.Api.OpenApi;

namespace CommercialNews.Api.CompositionRoot;

public static class HostRegistration
{
    public static IServiceCollection AddHostServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();
        services.AddRouting();
        services.AddControllers();

        services.AddHostHealthChecks();
        services.AddHostOpenApi();

        return services;
    }
}