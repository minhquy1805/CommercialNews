using Authorization.Application.Ports.Services;
using Identity.Application.Ports.Services;

namespace CommercialNews.Api.CompositionRoot;

public static class StartupInitializationExtensions
{
    public static async Task InitializeApplicationDataAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();

        var identityDataInitializer =
            scope.ServiceProvider.GetRequiredService<IIdentityDataInitializer>();

        await identityDataInitializer.InitializeAsync();

        var authorizationDataInitializer =
            scope.ServiceProvider.GetRequiredService<IAuthorizationDataInitializer>();

        await authorizationDataInitializer.InitializeAsync();
    }
}