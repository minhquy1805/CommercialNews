using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Outbox.Publishing;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Outbox.Handlers.Identity;

public sealed class IdentityUserSessionsRevokedOutboxHandler : IOutboxMessageHandler
{
    private readonly IOutboxEventPublisher _publisher;

    public IdentityUserSessionsRevokedOutboxHandler(
        IOutboxEventPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public string EventType =>
        IdentityIntegrationEventTypes.UserSessionsRevoked;

    public async Task<Result<DispatchOutboxMessageResult>> HandleAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        Result<PublishOutboxEventResult> publishResult =
            await _publisher.PublishAsync(
                outboxMessage,
                routingKey: IdentityIntegrationEventTypes.UserSessionsRevoked,
                cancellationToken);

        return publishResult.ToDispatchResult();
    }
}