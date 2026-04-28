using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Validation.MarkOutboxFailed;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxFailed;

/// <summary>
/// Marks a single outbox message as failed and optionally schedules a retry.
/// This is a write use case and opens a transaction.
/// </summary>
public sealed class MarkOutboxFailedUseCase : IMarkOutboxFailedUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IOutboxUnitOfWork _unitOfWork;

    public MarkOutboxFailedUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        IOutboxUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<MarkOutboxFailedResponse>> ExecuteAsync(
        MarkOutboxFailedRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = MarkOutboxFailedValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<MarkOutboxFailedResponse>.Failure(validationError);
        }

        try
        {
            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<MarkOutboxFailedResponse>.Failure(
                    OutboxErrors.Message.NotFound);
            }

            if (!CanMarkFailed(outboxMessage.Status))
            {
                return Result<MarkOutboxFailedResponse>.Failure(
                    OutboxErrors.Message.InvalidState);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _outboxMessageRepository.MarkFailedAsync(
                    _unitOfWork,
                    outboxMessage.OutboxMessageId,
                    request.NextRetryAt,
                    NormalizeOptional(request.LastError),
                    NormalizeOptional(request.LastErrorCode),
                    NormalizeOptional(request.LastErrorClass),
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<MarkOutboxFailedResponse>.Failure(
                        OutboxErrors.Message.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<MarkOutboxFailedResponse>.Success(
                    new MarkOutboxFailedResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = OutboxMessageStatus.Failed,
                        NextRetryAt = request.NextRetryAt
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
            return Result<MarkOutboxFailedResponse>.Failure(
                OutboxErrors.DependencyUnavailable);
        }
    }

    private static bool CanMarkFailed(string status)
    {
        return string.Equals(status, OutboxMessageStatus.Publishing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, OutboxMessageStatus.Failed, StringComparison.OrdinalIgnoreCase);
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