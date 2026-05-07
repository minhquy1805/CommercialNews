using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Outbox.Publishing;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Outbox.Handlers.Identity;

public sealed class IdentityUserDisabledOutboxHandler : IOutboxMessageHandler
{
    private readonly IOutboxEventPublisher _publisher;

    public IdentityUserDisabledOutboxHandler(
        IOutboxEventPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public string EventType =>
        IdentityIntegrationEventTypes.UserDisabled;

    public async Task<Result<DispatchOutboxMessageResult>> HandleAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        Result<PublishOutboxEventResult> publishResult =
            await _publisher.PublishAsync(
                outboxMessage,
                routingKey: IdentityIntegrationEventTypes.UserDisabled,
                cancellationToken);

        return publishResult.ToDispatchResult();
    }
}