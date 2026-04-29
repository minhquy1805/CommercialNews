using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Outbox.Publishing;
using Notifications.Application.Outbox;

namespace CommercialNews.Worker.Outbox.Handlers.Notifications;

public sealed class NotificationsEmailDeadOutboxHandler : IOutboxMessageHandler
{
    private readonly IOutboxEventPublisher _publisher;

    public NotificationsEmailDeadOutboxHandler(
        IOutboxEventPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public string EventType => NotificationsIntegrationEventTypes.EmailDead;

    public async Task<Result<DispatchOutboxMessageResult>> HandleAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        Result<PublishOutboxEventResult> publishResult =
            await _publisher.PublishAsync(
                message: outboxMessage,
                routingKey: EventType,
                cancellationToken: cancellationToken);

        return MapPublishResult(publishResult);
    }

    private static Result<DispatchOutboxMessageResult> MapPublishResult(
        Result<PublishOutboxEventResult> publishResult)
    {
        if (publishResult.IsFailure)
        {
            return Result<DispatchOutboxMessageResult>.Failure(
                publishResult.Error!);
        }

        PublishOutboxEventResult value = publishResult.Value;

        if (value.Succeeded)
        {
            return Result<DispatchOutboxMessageResult>.Success(
                DispatchOutboxMessageResult.Success());
        }

        return Result<DispatchOutboxMessageResult>.Success(
            DispatchOutboxMessageResult.Failed(
                errorCode: value.ErrorCode,
                errorMessage: value.ErrorMessage,
                errorClass: value.ErrorClass,
                isRetryable: value.IsRetryable,
                isAmbiguous: value.IsAmbiguous));
    }
}