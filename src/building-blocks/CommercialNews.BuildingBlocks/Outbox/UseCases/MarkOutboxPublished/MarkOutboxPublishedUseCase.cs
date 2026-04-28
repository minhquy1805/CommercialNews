using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Validation.MarkOutboxPublished;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxPublished;

/// <summary>
/// Marks a single outbox message as published after successful producer-side dispatch/handoff.
/// This does not mean downstream side effects have completed.
/// This is a write use case and opens a transaction.
/// </summary>
public sealed class MarkOutboxPublishedUseCase : IMarkOutboxPublishedUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IOutboxUnitOfWork _unitOfWork;

    public MarkOutboxPublishedUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        IOutboxUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<MarkOutboxPublishedResponse>> ExecuteAsync(
        MarkOutboxPublishedRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = MarkOutboxPublishedValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<MarkOutboxPublishedResponse>.Failure(validationError);
        }

        try
        {
            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<MarkOutboxPublishedResponse>.Failure(
                    OutboxErrors.Message.NotFound);
            }

            if (string.Equals(
                    outboxMessage.Status,
                    OutboxMessageStatus.Published,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result<MarkOutboxPublishedResponse>.Success(
                    new MarkOutboxPublishedResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = outboxMessage.Status
                    });
            }

            if (!CanMarkPublished(outboxMessage.Status))
            {
                return Result<MarkOutboxPublishedResponse>.Failure(
                    OutboxErrors.Message.InvalidState);
            }

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

                    return Result<MarkOutboxPublishedResponse>.Failure(
                        OutboxErrors.Message.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<MarkOutboxPublishedResponse>.Success(
                    new MarkOutboxPublishedResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
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
            return Result<MarkOutboxPublishedResponse>.Failure(
                OutboxErrors.DependencyUnavailable);
        }
    }

    private static bool CanMarkPublished(string status)
    {
        return string.Equals(status, OutboxMessageStatus.Publishing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, OutboxMessageStatus.Failed, StringComparison.OrdinalIgnoreCase);
    }
}