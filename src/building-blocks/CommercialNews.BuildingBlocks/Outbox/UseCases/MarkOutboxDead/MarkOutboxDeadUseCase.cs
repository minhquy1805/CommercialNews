using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Validation.MarkOutboxDead;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxDead;

/// <summary>
/// Marks a single outbox message as dead when it should no longer be retried.
/// This is a write use case and opens a transaction.
/// </summary>
public sealed class MarkOutboxDeadUseCase : IMarkOutboxDeadUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly ISqlUnitOfWork _unitOfWork;

    public MarkOutboxDeadUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        ISqlUnitOfWork unitOfWork)
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
                    OutboxErrors.Message.NotFound);
            }

            if (string.Equals(
                    outboxMessage.Status,
                    OutboxMessageStatus.Published,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result<MarkOutboxDeadResponse>.Failure(
                    OutboxErrors.Message.InvalidState);
            }

            if (string.Equals(
                    outboxMessage.Status,
                    OutboxMessageStatus.Dead,
                    StringComparison.OrdinalIgnoreCase))
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
                        OutboxErrors.Message.StaleWriteConflict);
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
        catch (PersistenceException)
        {
            return Result<MarkOutboxDeadResponse>.Failure(
                OutboxErrors.DependencyUnavailable);
        }
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