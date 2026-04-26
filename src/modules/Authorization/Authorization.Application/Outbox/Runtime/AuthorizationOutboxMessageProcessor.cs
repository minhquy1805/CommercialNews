using Authorization.Application.Ports.Persistence;
using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.Outbox.Runtime;

public sealed class AuthorizationOutboxMessageProcessor : IOutboxMessageProcessor
{
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMinutes(1);

    private readonly IOutboxDispatcher _dispatcher;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthorizationOutboxMessageProcessor(
        IOutboxDispatcher dispatcher,
        IOutboxMessageRepository outboxMessageRepository,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _outboxMessageRepository = outboxMessageRepository ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<ProcessOutboxMessageResult>> ProcessAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        if (outboxMessage is null)
        {
            return Result<ProcessOutboxMessageResult>.Failure(
                OutboxErrors.InvalidRequest);
        }

        if (!IsProcessableStatus(outboxMessage.Status))
        {
            return Result<ProcessOutboxMessageResult>.Failure(
                OutboxErrors.Message.InvalidState);
        }

        Result<DispatchOutboxMessageResult> dispatchResult =
            await _dispatcher.DispatchAsync(outboxMessage, cancellationToken);

        if (dispatchResult.IsFailure)
        {
            Error error = dispatchResult.Error!;

            return Result<ProcessOutboxMessageResult>.Success(
                new ProcessOutboxMessageResult
                {
                    OutboxMessageId = outboxMessage.OutboxMessageId,
                    MessageId = outboxMessage.MessageId,
                    EventType = outboxMessage.EventType,
                    Succeeded = false,
                    Status = outboxMessage.Status,
                    ErrorCode = error.Code,
                    ErrorMessage = error.Message
                });
        }

        DispatchOutboxMessageResult dispatchValue = dispatchResult.Value!;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (dispatchValue.Succeeded)
            {
                int affectedRows = await _outboxMessageRepository.MarkPublishedAsync(
                    _unitOfWork,
                    outboxMessage.OutboxMessageId,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<ProcessOutboxMessageResult>.Failure(
                        OutboxErrors.Message.StaleWriteConflict);
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

            if (dispatchValue.ShouldMarkDead)
            {
                int affectedRows = await _outboxMessageRepository.MarkDeadAsync(
                    _unitOfWork,
                    outboxMessage.OutboxMessageId,
                    NormalizeOptional(dispatchValue.ErrorMessage),
                    NormalizeOptional(dispatchValue.ErrorCode),
                    "Permanent",
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<ProcessOutboxMessageResult>.Failure(
                        OutboxErrors.Message.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ProcessOutboxMessageResult>.Success(
                    new ProcessOutboxMessageResult
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        EventType = outboxMessage.EventType,
                        Succeeded = false,
                        Status = OutboxMessageStatus.Dead,
                        ErrorCode = dispatchValue.ErrorCode,
                        ErrorMessage = dispatchValue.ErrorMessage
                    });
            }

            DateTime nextRetryAt = _dateTimeProvider.UtcNow.Add(DefaultRetryDelay);

            int failedRows = await _outboxMessageRepository.MarkFailedAsync(
                _unitOfWork,
                outboxMessage.OutboxMessageId,
                nextRetryAt,
                NormalizeOptional(dispatchValue.ErrorMessage),
                NormalizeOptional(dispatchValue.ErrorCode),
                "Transient",
                cancellationToken);

            if (failedRows <= 0)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<ProcessOutboxMessageResult>.Failure(
                    OutboxErrors.Message.StaleWriteConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProcessOutboxMessageResult>.Success(
                new ProcessOutboxMessageResult
                {
                    OutboxMessageId = outboxMessage.OutboxMessageId,
                    MessageId = outboxMessage.MessageId,
                    EventType = outboxMessage.EventType,
                    Succeeded = false,
                    Status = OutboxMessageStatus.Failed,
                    ErrorCode = dispatchValue.ErrorCode,
                    ErrorMessage = dispatchValue.ErrorMessage
                });
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsProcessableStatus(string? status)
    {
        return string.Equals(status, OutboxMessageStatus.Pending, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, OutboxMessageStatus.Failed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, OutboxMessageStatus.Publishing, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}