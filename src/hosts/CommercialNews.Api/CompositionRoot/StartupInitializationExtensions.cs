using CommercialNews.BuildingBlocks.Initialization;

namespace CommercialNews.Api.CompositionRoot;

public static class StartupInitializationExtensions
{
    public static async Task InitializeApplicationDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();

        var dataInitializationOrchestrator =
            scope.ServiceProvider.GetRequiredService<DataInitializationOrchestrator>();

        await dataInitializationOrchestrator.RunAsync(cancellationToken);
    }
}
