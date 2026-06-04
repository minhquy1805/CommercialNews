using Microsoft.Extensions.Logging;

namespace CommercialNews.BuildingBlocks.Initialization;

/// <summary>
/// Runs registered startup data initializers in deterministic order.
/// </summary>
public sealed class DataInitializationOrchestrator
{
    private readonly IEnumerable<IDataInitializer> _initializers;
    private readonly ILogger<DataInitializationOrchestrator> _logger;

    public DataInitializationOrchestrator(
        IEnumerable<IDataInitializer> initializers,
        ILogger<DataInitializationOrchestrator> logger)
    {
        _initializers = initializers ?? throw new ArgumentNullException(nameof(initializers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var orderedInitializers = _initializers
            .OrderBy(x => x.Order)
            .ToArray();

        foreach (var initializer in orderedInitializers)
        {
            _logger.LogInformation(
                "Running data initializer {InitializerType} with order {Order}.",
                initializer.GetType().FullName,
                initializer.Order);

            await initializer.InitializeAsync(cancellationToken);

            _logger.LogInformation(
                "Completed data initializer {InitializerType}.",
                initializer.GetType().FullName);
        }
    }
}
