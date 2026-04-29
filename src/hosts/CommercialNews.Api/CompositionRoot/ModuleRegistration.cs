using Audit.Application.DependencyInjection;
using Audit.Infrastructure.DependencyInjection;
using Authorization.Application.DependencyInjection;
using Authorization.Infrastructure.DependencyInjection;
using Content.Application.DependencyInjection;
using Content.Infrastructure.DependencyInjection;
using Identity.Application.DependencyInjection;
using Identity.Infrastructure.DependencyInjection;
using Interaction.Application.DependencyInjection;
using Interaction.Infrastructure.DependencyInjection;
using Media.Application.DependencyInjection;
using Media.Infrastructure.DependencyInjection;
using Notifications.Application.DependencyInjection;
using Notifications.Infrastructure.DependencyInjection;
using Reading.Application.DependencyInjection;
using Reading.Infrastructure.DependencyInjection;
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

        services.AddIdentityApplication(configuration);
        services.AddIdentityInfrastructure(configuration);

        services.AddAuthorizationApplication();
        services.AddAuthorizationInfrastructure(configuration);

        services.AddMediaApplication();
        services.AddMediaInfrastructure();

        services.AddSeoApplication();
        services.AddSeoInfrastructure();

        services.AddReadingApplication();
        services.AddReadingInfrastructure();

        services.AddInteractionApplication();
        services.AddInteractionInfrastructure();

        services.AddNotificationsApplication(configuration);
        services.AddNotificationsInfrastructure(configuration);

        services.AddAuditApplication();
        services.AddAuditInfrastructure();

        return services;
    }
}