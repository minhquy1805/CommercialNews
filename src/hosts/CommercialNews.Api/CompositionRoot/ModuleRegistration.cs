using Content.Application.DependencyInjection;
using Content.Infrastructure.DependencyInjection;
using Identity.Application.DependencyInjection;
using Identity.Infrastructure.DependencyInjection;

namespace CommercialNews.Api.CompositionRoot;

public static class ModuleRegistration
{
    public static IServiceCollection AddApplicationModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddContentApplication();
        services.AddContentInfrastructure();

        // TODO:
        // Move module registrations here gradually after each module is refactored
        // into a consistent Application/Infrastructure registration style.
        //
        // Example future state:
        // services.AddIdentityApplication();
        // services.AddIdentityInfrastructure(configuration);
        //
        // services.AddAuthorizationApplication();
        // services.AddAuthorizationInfrastructure(configuration);

        services.AddIdentityApplication();
        services.AddIdentityInfrastructure();

        return services;
    }
}