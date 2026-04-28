using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private readonly IReadOnlyDictionary<string, IOutboxMessageHandler> _handlersByEventType;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IEnumerable<IOutboxMessageHandler> handlers,
        ILogger<OutboxDispatcher> logger)
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

    public async Task<Result<DispatchOutboxMessageResult>> DispatchAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        string eventType = NormalizeEventType(outboxMessage.EventType);

        if (eventType.Length == 0)
        {
            return Result<DispatchOutboxMessageResult>.Failure(
                Error.Validation(
                    code: "OUTBOX.EVENT_TYPE_REQUIRED",
                    message: "Outbox event type is required."));
        }

        if (!_handlersByEventType.TryGetValue(eventType, out IOutboxMessageHandler? handler))
        {
            _logger.LogWarning(
                "No outbox handler registered for EventType={EventType}. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}",
                outboxMessage.EventType,
                outboxMessage.OutboxMessageId,
                outboxMessage.MessageId);

            return Result<DispatchOutboxMessageResult>.Success(
                DispatchOutboxMessageResult.Failed(
                    errorCode: "OUTBOX.HANDLER_NOT_FOUND",
                    errorMessage: $"No outbox handler registered for event type '{outboxMessage.EventType}'.",
                    errorClass: OutboxFailureClass.Permanent,
                    isRetryable: false));
        }

        try
        {
            return await handler.HandleAsync(
                outboxMessage,
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
                "Unhandled exception from outbox handler. EventType={EventType}, OutboxMessageId={OutboxMessageId}, MessageId={MessageId}",
                outboxMessage.EventType,
                outboxMessage.OutboxMessageId,
                outboxMessage.MessageId);

            return Result<DispatchOutboxMessageResult>.Success(
                DispatchOutboxMessageResult.Failed(
                    errorCode: "OUTBOX.HANDLER_EXCEPTION",
                    errorMessage: exception.Message,
                    errorClass: OutboxFailureClass.Unknown,
                    isRetryable: true));
        }
    }

    private static string NormalizeEventType(string? eventType)
    {
        return string.IsNullOrWhiteSpace(eventType)
            ? string.Empty
            : eventType.Trim();
    }
}