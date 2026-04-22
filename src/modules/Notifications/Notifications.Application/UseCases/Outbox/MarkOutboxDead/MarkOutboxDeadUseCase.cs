using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Transactions;
using Notifications.Application.Validation.Outbox.MarkOutboxDead;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxDead;

/// <summary>
/// Marks a single outbox message as dead when it should no longer be retried.
/// This is a write use case and opens a transaction.
/// </summary>
public sealed class MarkOutboxDeadUseCase : IMarkOutboxDeadUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;

    public MarkOutboxDeadUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        INotificationsUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<MarkOutboxDeadResponse>> ExecuteAsync(
        MarkOutboxDeadRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = MarkOutboxDeadValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<MarkOutboxDeadResponse>.Failure(validationError);
        }

        try
        {
            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<MarkOutboxDeadResponse>.Failure(
                    NotificationsErrors.Outbox.NotFound);
            }

            if (string.Equals(outboxMessage.Status, OutboxMessageStatus.Published, StringComparison.OrdinalIgnoreCase))
            {
                return Result<MarkOutboxDeadResponse>.Failure(
                    NotificationsErrors.Outbox.InvalidState);
            }

            if (string.Equals(outboxMessage.Status, OutboxMessageStatus.Dead, StringComparison.OrdinalIgnoreCase))
            {
                return Result<MarkOutboxDeadResponse>.Success(
                    new MarkOutboxDeadResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = outboxMessage.Status
                    });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _outboxMessageRepository.MarkDeadAsync(
                    _unitOfWork,
                    outboxMessage.OutboxMessageId,
                    NormalizeOptional(request.LastError),
                    NormalizeOptional(request.LastErrorCode),
                    NormalizeOptional(request.LastErrorClass),
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<MarkOutboxDeadResponse>.Failure(
                        NotificationsErrors.Outbox.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<MarkOutboxDeadResponse>.Success(
                    new MarkOutboxDeadResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = OutboxMessageStatus.Dead
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<MarkOutboxDeadResponse>.Failure(
                MapPersistenceException(exception));
        }
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "OUTBOX.MESSAGE_NOT_FOUND" => NotificationsErrors.Outbox.NotFound,
            "OUTBOX.MESSAGE_STALE_WRITE_CONFLICT" => NotificationsErrors.Outbox.StaleWriteConflict,
            _ => NotificationsErrors.DependencyUnavailable
        };
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