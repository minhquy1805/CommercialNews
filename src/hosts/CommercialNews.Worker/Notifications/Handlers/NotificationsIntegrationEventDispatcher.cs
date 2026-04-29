using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Worker.Notifications.Handlers;

public sealed class NotificationsIntegrationEventDispatcher
{
    private readonly IReadOnlyDictionary<string, INotificationsIntegrationEventHandler> _handlersByEventType;
    private readonly ILogger<NotificationsIntegrationEventDispatcher> _logger;

    public NotificationsIntegrationEventDispatcher(
        IEnumerable<INotificationsIntegrationEventHandler> handlers,
        ILogger<NotificationsIntegrationEventDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _handlersByEventType = handlers
            .GroupBy(handler => NormalizeEventType(handler.EventType), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Single(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Result> DispatchAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        string eventType = NormalizeEventType(envelope.EventType);

        if (eventType.Length == 0)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.EVENT_TYPE_REQUIRED",
                    message: "Integration event type is required."));
        }

        if (!_handlersByEventType.TryGetValue(eventType, out INotificationsIntegrationEventHandler? handler))
        {
            _logger.LogWarning(
                "No notifications integration event handler registered. EventType={EventType}, MessageId={MessageId}, CorrelationId={CorrelationId}",
                envelope.EventType,
                envelope.MessageId,
                envelope.CorrelationId);

            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.HANDLER_NOT_FOUND",
                    message: $"No notifications integration event handler registered for event type '{envelope.EventType}'."));
        }

        return await handler.HandleAsync(envelope, cancellationToken);
    }

    private static string NormalizeEventType(string? eventType)
    {
        return string.IsNullOrWhiteSpace(eventType)
            ? string.Empty
            : eventType.Trim();
    }
}