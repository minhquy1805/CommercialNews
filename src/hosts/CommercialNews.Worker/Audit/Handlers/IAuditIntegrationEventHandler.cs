using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Worker.Audit.Handlers;

public interface IAuditIntegrationEventHandler
{
    string EventType { get; }

    Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default);
}