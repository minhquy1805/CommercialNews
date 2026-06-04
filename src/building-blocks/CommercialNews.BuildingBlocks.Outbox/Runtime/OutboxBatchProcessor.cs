using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public sealed class OutboxBatchProcessor : IOutboxBatchProcessor
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IOutboxMessageProcessor _messageProcessor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutboxBatchProcessor(
        IOutboxMessageRepository outboxMessageRepository,
        IOutboxMessageProcessor messageProcessor,
        IDateTimeProvider dateTimeProvider)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _messageProcessor = messageProcessor
            ?? throw new ArgumentNullException(nameof(messageProcessor));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<ProcessPendingOutboxMessagesResponse>> ProcessAsync(
        ProcessPendingOutboxMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.BatchSize <= 0)
        {
            return Result<ProcessPendingOutboxMessagesResponse>.Failure(
                Error.Validation(
                    code: "OUTBOX.INVALID_REQUEST",
                    message: "BatchSize must be greater than zero."));
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            IReadOnlyList<OutboxMessage> claimedMessages =
                await _outboxMessageRepository.ClaimPendingAsync(
                    request.BatchSize,
                    nowUtc,
                    cancellationToken);

            List<ProcessPendingOutboxMessageItemResult> items = [];
            int succeededCount = 0;
            int failedCount = 0;

            foreach (OutboxMessage claimedMessage in claimedMessages)
            {
                Result<ProcessOutboxMessageResult> singleResult =
                    await _messageProcessor.ProcessAsync(
                        claimedMessage,
                        cancellationToken);

                ProcessPendingOutboxMessageItemResult item =
                    ToBatchItem(claimedMessage, singleResult);

                items.Add(item);

                if (item.Succeeded)
                {
                    succeededCount++;
                }
                else
                {
                    failedCount++;
                }

                if (request.StopOnFirstFailure && !item.Succeeded)
                {
                    break;
                }
            }

            return Result<ProcessPendingOutboxMessagesResponse>.Success(
                new ProcessPendingOutboxMessagesResponse
                {
                    RequestedBatchSize = request.BatchSize,
                    ClaimedCount = claimedMessages.Count,
                    ProcessedCount = items.Count,
                    SucceededCount = succeededCount,
                    FailedCount = failedCount,
                    Items = items
                });
        }
        catch (PersistenceException)
        {
            return Result<ProcessPendingOutboxMessagesResponse>.Failure(
                Error.Failure(
                    code: "OUTBOX.DEPENDENCY_UNAVAILABLE",
                    message: "Outbox persistence dependency is unavailable."));
        }
    }

    private static ProcessPendingOutboxMessageItemResult ToBatchItem(
        OutboxMessage claimedMessage,
        Result<ProcessOutboxMessageResult> singleResult)
    {
        if (singleResult.IsSuccess)
        {
            ProcessOutboxMessageResult value = singleResult.Value!;

            return new ProcessPendingOutboxMessageItemResult
            {
                OutboxMessageId = value.OutboxMessageId,
                MessageId = value.MessageId,
                EventType = value.EventType,
                Succeeded = value.Succeeded,
                Status = value.Status,
                ErrorCode = value.ErrorCode,
                ErrorMessage = value.ErrorMessage
            };
        }

        Error error = singleResult.Error!;

        return new ProcessPendingOutboxMessageItemResult
        {
            OutboxMessageId = claimedMessage.OutboxMessageId,
            MessageId = claimedMessage.MessageId,
            EventType = claimedMessage.EventType,
            Succeeded = false,
            Status = claimedMessage.Status,
            ErrorCode = error.Code,
            ErrorMessage = error.Message
        };
    }
}