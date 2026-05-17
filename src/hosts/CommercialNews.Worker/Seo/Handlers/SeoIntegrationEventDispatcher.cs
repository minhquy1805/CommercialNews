using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Worker.Seo.Handlers;

public sealed class SeoIntegrationEventDispatcher
{
    private readonly IReadOnlyDictionary<string, ISeoIntegrationEventHandler> _handlersByEventType;
    private readonly ILogger<SeoIntegrationEventDispatcher> _logger;

    public SeoIntegrationEventDispatcher(
        IEnumerable<ISeoIntegrationEventHandler> handlers,
        ILogger<SeoIntegrationEventDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ISeoIntegrationEventHandler[] handlerArray = handlers.ToArray();

        ValidateHandlers(handlerArray);

        _handlersByEventType = handlerArray.ToDictionary(
            handler => NormalizeEventType(handler.EventType),
            handler => handler,
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
                    code: "SEO.EVENT_TYPE_REQUIRED",
                    message: "SEO integration event type is required."));
        }

        if (!_handlersByEventType.TryGetValue(eventType, out ISeoIntegrationEventHandler? handler))
        {
            _logger.LogWarning(
                "No SEO integration event handler registered. EventType={EventType}, MessageId={MessageId}, AggregateType={AggregateType}, AggregateId={AggregateId}",
                envelope.EventType,
                envelope.MessageId,
                envelope.AggregateType,
                envelope.AggregateId);

            return Result.Failure(
                Error.Validation(
                    code: "SEO.UNSUPPORTED_EVENT_TYPE",
                    message: $"SEO integration event type '{envelope.EventType}' is not supported."));
        }

        try
        {
            return await handler.HandleAsync(
                envelope,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception from SEO integration event handler. EventType={EventType}, MessageId={MessageId}, AggregateType={AggregateType}, AggregateId={AggregateId}",
                envelope.EventType,
                envelope.MessageId,
                envelope.AggregateType,
                envelope.AggregateId);

            return Result.Failure(
                Error.Failure(
                    code: "SEO.HANDLER_EXCEPTION",
                    message: "SEO integration event handler failed unexpectedly."));
        }
    }

    private static void ValidateHandlers(
        IReadOnlyCollection<ISeoIntegrationEventHandler> handlers)
    {
        string[] emptyEventTypeHandlers = handlers
            .Where(handler => string.IsNullOrWhiteSpace(handler.EventType))
            .Select(handler => handler.GetType().Name)
            .ToArray();

        if (emptyEventTypeHandlers.Length > 0)
        {
            throw new InvalidOperationException(
                "SEO integration event handlers must declare a non-empty EventType. Invalid handlers: " +
                string.Join(", ", emptyEventTypeHandlers));
        }

        string[] duplicateEventTypes = handlers
            .GroupBy(
                handler => NormalizeEventType(handler.EventType),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateEventTypes.Length > 0)
        {
            throw new InvalidOperationException(
                "Duplicate SEO integration event handlers registered for event types: " +
                string.Join(", ", duplicateEventTypes));
        }
    }

    private static string NormalizeEventType(string? eventType)
    {
        return string.IsNullOrWhiteSpace(eventType)
            ? string.Empty
            : eventType.Trim();
    }
}