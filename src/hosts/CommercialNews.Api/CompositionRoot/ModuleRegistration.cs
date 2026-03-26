namespace CommercialNews.Api.CompositionRoot;

public static class ModuleRegistration
{
    public static IServiceCollection AddApplicationModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

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
        //
        // services.AddContentApplication();
        // services.AddContentInfrastructure(configuration);

        return services;
    }
}