using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.Outbox.Runtime;

public sealed class AuthorizationOutboxBatchProcessor : IOutboxBatchProcessor
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IOutboxMessageProcessor _messageProcessor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthorizationOutboxBatchProcessor(
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

            if (singleResult.IsSuccess)
            {
                ProcessOutboxMessageResult value = singleResult.Value!;

                if (value.Succeeded)
                {
                    succeededCount++;
                }
                else
                {
                    failedCount++;
                }

                items.Add(new ProcessPendingOutboxMessageItemResult
                {
                    OutboxMessageId = value.OutboxMessageId,
                    MessageId = value.MessageId,
                    EventType = value.EventType,
                    Succeeded = value.Succeeded,
                    Status = value.Status,
                    ErrorCode = value.ErrorCode,
                    ErrorMessage = value.ErrorMessage
                });
            }
            else
            {
                failedCount++;

                Error error = singleResult.Error!;

                items.Add(new ProcessPendingOutboxMessageItemResult
                {
                    OutboxMessageId = claimedMessage.OutboxMessageId,
                    MessageId = claimedMessage.MessageId,
                    EventType = claimedMessage.EventType,
                    Succeeded = false,
                    Status = claimedMessage.Status,
                    ErrorCode = error.Code,
                    ErrorMessage = error.Message
                });
            }

            if (request.StopOnFirstFailure && items[^1].Succeeded is false)
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
}