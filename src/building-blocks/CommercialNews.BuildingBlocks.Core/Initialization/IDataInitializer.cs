namespace CommercialNews.BuildingBlocks.Initialization;

/// <summary>
/// Base contract for startup data initialization.
/// </summary>
public interface IDataInitializer
{
    /// <summary>
    /// Defines execution order.
    /// Lower values run earlier.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Executes initialization logic.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
