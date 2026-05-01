using Authorization.Application.Outbox;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Outbox.Publishing;

namespace CommercialNews.Worker.Outbox.Handlers.Authorization;

public sealed class AuthorizationRoleUpdatedOutboxHandler : IOutboxMessageHandler
{
    private readonly IOutboxEventPublisher _publisher;

    public AuthorizationRoleUpdatedOutboxHandler(
        IOutboxEventPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public string EventType =>
        AuthorizationIntegrationEventTypes.RoleUpdated;

    public async Task<Result<DispatchOutboxMessageResult>> HandleAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        Result<PublishOutboxEventResult> publishResult =
            await _publisher.PublishAsync(
                outboxMessage,
                routingKey: AuthorizationIntegrationEventTypes.RoleUpdated,
                cancellationToken);

        return publishResult.ToDispatchResult();
    }
}