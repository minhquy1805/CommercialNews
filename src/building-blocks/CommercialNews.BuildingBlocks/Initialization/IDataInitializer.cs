namespace CommercialNews.BuildingBlocks.Initialization;

/// <summary>
/// Base contract for startup data initialization.
/// 
/// Intended for future cross-module use when the application has multiple
/// data initializers that should be executed in a deterministic order.
/// 
/// Current note:
/// - This contract is introduced early to make later refactoring easier.
/// - Existing module-specific initializers can continue to use their own
///   dedicated interfaces for now.
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