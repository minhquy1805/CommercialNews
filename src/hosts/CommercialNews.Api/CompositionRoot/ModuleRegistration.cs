using Authorization.Application.DependencyInjection;
using Authorization.Infrastructure.DependencyInjection;
using Content.Application.DependencyInjection;
using Content.Infrastructure.DependencyInjection;
using Identity.Application.DependencyInjection;
using Identity.Infrastructure.DependencyInjection;
using Media.Application.DependencyInjection;
using Media.Infrastructure.DependencyInjection;
using Seo.Application.DependencyInjection;
using Seo.Infrastructure.DependencyInjection;

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

        services.AddMediaApplication();
        services.AddMediaInfrastructure();

        services.AddSeoApplication();
        services.AddSeoInfrastructure();

        return services;
    }
}