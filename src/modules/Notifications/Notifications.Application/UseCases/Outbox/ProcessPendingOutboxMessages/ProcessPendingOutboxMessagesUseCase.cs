using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;
using Notifications.Application.Errors;
using Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;
using Notifications.Application.Validation.Outbox.ProcessPendingOutboxMessages;

namespace Notifications.Application.UseCases.Outbox.ProcessPendingOutboxMessages;

/// <summary>
/// Phase note:
/// This batch use case currently orchestrates Notifications-owned downstream
/// processing for shared outbox messages. If shared outbox dispatch becomes
/// a cross-module platform concern, move this orchestration into a shared
/// dispatcher/runtime layer in building-blocks.
/// </summary>
public sealed class ProcessPendingOutboxMessagesUseCase : IProcessPendingOutboxMessagesUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IProcessOutboxMessageUseCase _processOutboxMessageUseCase;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProcessPendingOutboxMessagesUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        IProcessOutboxMessageUseCase processOutboxMessageUseCase,
        IDateTimeProvider dateTimeProvider)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _processOutboxMessageUseCase = processOutboxMessageUseCase
            ?? throw new ArgumentNullException(nameof(processOutboxMessageUseCase));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<ProcessPendingOutboxMessagesResponse>> ExecuteAsync(
        ProcessPendingOutboxMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ProcessPendingOutboxMessagesValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ProcessPendingOutboxMessagesResponse>.Failure(validationError);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            var claimedMessages = await _outboxMessageRepository.ClaimPendingAsync(
                request.BatchSize,
                nowUtc,
                cancellationToken);

            List<ProcessPendingOutboxMessageItemResponse> items = [];
            int succeededCount = 0;
            int failedCount = 0;

            foreach (var claimedMessage in claimedMessages)
            {
                var singleResult = (await _processOutboxMessageUseCase.ExecuteAsync(
                    new ProcessOutboxMessageRequest
                    {
                        OutboxMessageId = claimedMessage.OutboxMessageId
                    },
                    cancellationToken))!;

                if (singleResult.IsSuccess)
                {
                    var successResponse = singleResult.Value!;
                    succeededCount++;

                    items.Add(new ProcessPendingOutboxMessageItemResponse
                    {
                        OutboxMessageId = successResponse.OutboxMessageId,
                        MessageId = successResponse.MessageId,
                        EventType = successResponse.EventType,
                        Succeeded = true,
                        CreatedEmailDelivery = successResponse.CreatedEmailDelivery,
                        EmailDeliveryId = successResponse.EmailDeliveryId,
                        Status = successResponse.Status
                    });

                    continue;
                }

                var error = singleResult.Error!;
                failedCount++;

                items.Add(new ProcessPendingOutboxMessageItemResponse
                {
                    OutboxMessageId = claimedMessage.OutboxMessageId,
                    MessageId = claimedMessage.MessageId,
                    EventType = claimedMessage.EventType,
                    Succeeded = false,
                    CreatedEmailDelivery = false,
                    EmailDeliveryId = null,
                    Status = claimedMessage.Status,
                    ErrorCode = error.Code,
                    ErrorMessage = error.Message
                });

                if (request.StopOnFirstFailure)
                {
                    break;
                }
            }

            ProcessPendingOutboxMessagesResponse response = new()
            {
                RequestedBatchSize = request.BatchSize,
                ClaimedCount = claimedMessages.Count,
                ProcessedCount = items.Count,
                SucceededCount = succeededCount,
                FailedCount = failedCount,
                Items = items
            };

            return Result<ProcessPendingOutboxMessagesResponse>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<ProcessPendingOutboxMessagesResponse>.Failure(
                NotificationsErrors.DependencyUnavailable with
                {
                    Details = new[]
                    {
                        $"persistence_code: {exception.Code}",
                        $"persistence_message: {exception.Message}"
                    }
                });
        }
    }
}