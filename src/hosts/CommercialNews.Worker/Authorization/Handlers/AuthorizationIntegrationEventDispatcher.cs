using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Worker.Authorization.Handlers;

public sealed class AuthorizationIntegrationEventDispatcher
{
    private readonly IReadOnlyDictionary<string, IAuthorizationIntegrationEventHandler> _handlersByEventType;
    private readonly ILogger<AuthorizationIntegrationEventDispatcher> _logger;

    public AuthorizationIntegrationEventDispatcher(
        IEnumerable<IAuthorizationIntegrationEventHandler> handlers,
        ILogger<AuthorizationIntegrationEventDispatcher> logger)
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
                    code: "AUTHORIZATION.EVENT_TYPE_REQUIRED",
                    message: "Integration event type is required."));
        }

        if (!_handlersByEventType.TryGetValue(eventType, out IAuthorizationIntegrationEventHandler? handler))
        {
            _logger.LogWarning(
                "No authorization integration event handler registered. EventType={EventType}, MessageId={MessageId}, CorrelationId={CorrelationId}",
                envelope.EventType,
                envelope.MessageId,
                envelope.CorrelationId);

            return Result.Failure(
                Error.Validation(
                    code: "AUTHORIZATION.HANDLER_NOT_FOUND",
                    message: $"No authorization integration event handler registered for event type '{envelope.EventType}'."));
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
