using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public sealed class OutboxMessageProcessor : IOutboxMessageProcessor
{
    private readonly IOutboxDispatcher _dispatcher;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOptions<OutboxProcessingOptions> _options;
    private readonly ILogger<OutboxMessageProcessor> _logger;

    public OutboxMessageProcessor(
        IOutboxDispatcher dispatcher,
        IOutboxMessageRepository outboxMessageRepository,
        IOutboxUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IOptions<OutboxProcessingOptions> options,
        ILogger<OutboxMessageProcessor> logger)
    {
        _dispatcher = dispatcher
            ?? throw new ArgumentNullException(nameof(dispatcher));

        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        _options = options
            ?? throw new ArgumentNullException(nameof(options));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ProcessOutboxMessageResult>> ProcessAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        if (!string.Equals(
                outboxMessage.Status,
                OutboxMessageStatus.Publishing,
                StringComparison.OrdinalIgnoreCase))
        {
            return Result<ProcessOutboxMessageResult>.Success(
                new ProcessOutboxMessageResult
                {
                    OutboxMessageId = outboxMessage.OutboxMessageId,
                    MessageId = outboxMessage.MessageId,
                    EventType = outboxMessage.EventType,
                    Succeeded = false,
                    Status = outboxMessage.Status,
                    ErrorCode = "OUTBOX.INVALID_STATE",
                    ErrorMessage = "Outbox message must be in Publishing state before processing."
                });
        }

        try
        {
            Result<DispatchOutboxMessageResult> dispatchResult =
                await _dispatcher.DispatchAsync(
                    outboxMessage,
                    cancellationToken);

            if (dispatchResult.IsFailure)
            {
                Error error = dispatchResult.Error!;

                return await MarkFailedOrDeadAsync(
                    outboxMessage,
                    errorCode: error.Code,
                    errorMessage: error.Message,
                    errorClass: OutboxFailureClass.Unknown,
                    isRetryable: true,
                    cancellationToken);
            }

            DispatchOutboxMessageResult dispatch = dispatchResult.Value!;

            if (dispatch.Succeeded)
            {
                return await MarkPublishedAsync(
                    outboxMessage,
                    cancellationToken);
            }

            return await MarkFailedOrDeadAsync(
                outboxMessage,
                dispatch.ErrorCode,
                dispatch.ErrorMessage,
                NormalizeFailureClass(dispatch.ErrorClass),
                dispatch.IsRetryable || dispatch.IsAmbiguous,
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
                "Unhandled exception while dispatching outbox message. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, EventType={EventType}",
                outboxMessage.OutboxMessageId,
                outboxMessage.MessageId,
                outboxMessage.EventType);

            return await MarkFailedOrDeadAsync(
                outboxMessage,
                errorCode: "OUTBOX.DISPATCH_EXCEPTION",
                errorMessage: exception.Message,
                errorClass: OutboxFailureClass.Unknown,
                isRetryable: true,
                cancellationToken);
        }
    }

    private async Task<Result<ProcessOutboxMessageResult>> MarkPublishedAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _outboxMessageRepository.MarkPublishedAsync(
                    _unitOfWork,
                    outboxMessage.OutboxMessageId,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<ProcessOutboxMessageResult>.Success(
                        new ProcessOutboxMessageResult
                        {
                            OutboxMessageId = outboxMessage.OutboxMessageId,
                            MessageId = outboxMessage.MessageId,
                            EventType = outboxMessage.EventType,
                            Succeeded = false,
                            Status = outboxMessage.Status,
                            ErrorCode = "OUTBOX.STALE_WRITE_CONFLICT",
                            ErrorMessage = "Outbox message state changed before it could be marked as Published."
                        });
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ProcessOutboxMessageResult>.Success(
                    new ProcessOutboxMessageResult
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        EventType = outboxMessage.EventType,
                        Succeeded = true,
                        Status = OutboxMessageStatus.Published
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<ProcessOutboxMessageResult>.Failure(
                Error.Failure(
                    code: "OUTBOX.DEPENDENCY_UNAVAILABLE",
                    message: "Outbox persistence dependency is unavailable."));
        }
    }

    private async Task<Result<ProcessOutboxMessageResult>> MarkFailedOrDeadAsync(
        OutboxMessage outboxMessage,
        string? errorCode,
        string? errorMessage,
        string? errorClass,
        bool isRetryable,
        CancellationToken cancellationToken)
    {
        try
        {
            OutboxProcessingOptions options = _options.Value;

            bool shouldMarkDead =
                !isRetryable ||
                outboxMessage.AttemptCount + 1 >= options.MaxRetryAttempts;

            DateTime? nextRetryAt = shouldMarkDead
                ? null
                : CalculateNextRetryAt(
                    _dateTimeProvider.UtcNow,
                    outboxMessage.AttemptCount,
                    options);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows;

                if (shouldMarkDead)
                {
                    affectedRows = await _outboxMessageRepository.MarkDeadAsync(
                        _unitOfWork,
                        outboxMessage.OutboxMessageId,
                        NormalizeError(errorMessage),
                        NormalizeErrorCode(errorCode),
                        NormalizeFailureClass(errorClass),
                        cancellationToken);
                }
                else
                {
                    affectedRows = await _outboxMessageRepository.MarkFailedAsync(
                        _unitOfWork,
                        outboxMessage.OutboxMessageId,
                        nextRetryAt,
                        NormalizeError(errorMessage),
                        NormalizeErrorCode(errorCode),
                        NormalizeFailureClass(errorClass),
                        cancellationToken);
                }

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<ProcessOutboxMessageResult>.Success(
                        new ProcessOutboxMessageResult
                        {
                            OutboxMessageId = outboxMessage.OutboxMessageId,
                            MessageId = outboxMessage.MessageId,
                            EventType = outboxMessage.EventType,
                            Succeeded = false,
                            Status = outboxMessage.Status,
                            ErrorCode = "OUTBOX.STALE_WRITE_CONFLICT",
                            ErrorMessage = "Outbox message state changed before failure state could be persisted."
                        });
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                string status = shouldMarkDead
                    ? OutboxMessageStatus.Dead
                    : OutboxMessageStatus.Failed;

                return Result<ProcessOutboxMessageResult>.Success(
                    new ProcessOutboxMessageResult
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        EventType = outboxMessage.EventType,
                        Succeeded = false,
                        Status = status,
                        ErrorCode = NormalizeErrorCode(errorCode),
                        ErrorMessage = NormalizeError(errorMessage)
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<ProcessOutboxMessageResult>.Failure(
                Error.Failure(
                    code: "OUTBOX.DEPENDENCY_UNAVAILABLE",
                    message: "Outbox persistence dependency is unavailable."));
        }
    }

    private static DateTime CalculateNextRetryAt(
        DateTime nowUtc,
        int currentAttemptCount,
        OutboxProcessingOptions options)
    {
        int attemptNumber = Math.Max(1, currentAttemptCount + 1);

        double delaySeconds = options.InitialRetryDelaySeconds *
                              Math.Pow(2, attemptNumber - 1);

        int cappedDelaySeconds = Math.Min(
            (int)delaySeconds,
            Math.Max(1, options.MaxRetryDelaySeconds));

        return nowUtc.AddSeconds(cappedDelaySeconds);
    }

    private static string? NormalizeError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        return normalized.Length <= 2000
            ? normalized
            : normalized[..2000];
    }

    private static string? NormalizeErrorCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        return normalized.Length <= 100
            ? normalized
            : normalized[..100];
    }

    private static string? NormalizeFailureClass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OutboxFailureClass.Unknown;
        }

        string normalized = value.Trim();

        if (!OutboxFailureClass.IsValid(normalized))
        {
            return OutboxFailureClass.Unknown;
        }

        return normalized.Length <= 30
            ? normalized
            : normalized[..30];
    }
}