using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Worker.Interaction.Handlers;

public interface IInteractionIntegrationEventHandler
{
    string EventType { get; }

    Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default);
}
