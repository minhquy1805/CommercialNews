using Authorization.Application.DependencyInjection;
using Authorization.Infrastructure.DependencyInjection;
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


        services.AddIdentityApplication();
        services.AddIdentityInfrastructure();

        services.AddAuthorizationApplication();
        services.AddAuthorizationInfrastructure();

        return services;
    }
}