// using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
// using CommercialNews.BuildingBlocks.SharedKernel.Results;
// using Notifications.Application.Contracts.Outbox.Requests;
// using Notifications.Application.Contracts.Outbox.Responses;
// using Notifications.Application.Errors;
// using Notifications.Application.Models.QueryModels;
// using Notifications.Application.Ports.Persistence.Read;

// namespace Notifications.Application.UseCases.Outbox.GetOutboxMessageByMessageId;

// /// <summary>
// /// Returns a detailed read-only view of a single outbox message by message id
// /// for tracing and notification runtime troubleshooting.
// /// This is a read use case, so it does not open a transaction.
// /// </summary>
// public sealed class GetOutboxMessageByMessageIdUseCase : IGetOutboxMessageByMessageIdUseCase
// {
//     private readonly IOutboxMessageQueryRepository _outboxMessageQueryRepository;

//     public GetOutboxMessageByMessageIdUseCase(
//         IOutboxMessageQueryRepository outboxMessageQueryRepository)
//     {
//         _outboxMessageQueryRepository = outboxMessageQueryRepository
//             ?? throw new ArgumentNullException(nameof(outboxMessageQueryRepository));
//     }

//     public async Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
//         GetOutboxMessageByMessageIdRequest request,
//         CancellationToken cancellationToken = default)
//     {
//         try
//         {
//             if (string.IsNullOrWhiteSpace(request.MessageId))
//             {
//                 return Result<GetOutboxMessageByIdResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.MessageIdRequired);
//             }

//             string messageId = request.MessageId.Trim();

//             if (messageId.Length > 26)
//             {
//                 return Result<GetOutboxMessageByIdResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.MessageIdTooLong);
//             }

//             OutboxMessageResult? outboxMessage =
//                 await _outboxMessageQueryRepository.GetByMessageIdAsync(
//                     messageId,
//                     cancellationToken);

//             if (outboxMessage is null)
//             {
//                 return Result<GetOutboxMessageByIdResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.NotFound);
//             }

//             GetOutboxMessageByIdResponse response = new()
//             {
//                 OutboxMessageId = outboxMessage.OutboxMessageId,
//                 MessageId = outboxMessage.MessageId,
//                 EventType = outboxMessage.EventType,
//                 AggregateType = outboxMessage.AggregateType,
//                 AggregateId = outboxMessage.AggregateId,
//                 AggregatePublicId = outboxMessage.AggregatePublicId,
//                 AggregateVersion = outboxMessage.AggregateVersion,
//                 CorrelationId = outboxMessage.CorrelationId,
//                 InitiatorUserId = outboxMessage.InitiatorUserId,
//                 Priority = outboxMessage.Priority,
//                 Status = outboxMessage.Status,
//                 AttemptCount = outboxMessage.AttemptCount,
//                 NextRetryAt = outboxMessage.NextRetryAt,
//                 LastAttemptAt = outboxMessage.LastAttemptAt,
//                 PublishedAt = outboxMessage.PublishedAt,
//                 LastErrorCode = outboxMessage.LastErrorCode,
//                 LastErrorClass = outboxMessage.LastErrorClass,
//                 OccurredAt = outboxMessage.OccurredAt,
//                 CreatedAt = outboxMessage.CreatedAt,
//                 UpdatedAt = outboxMessage.UpdatedAt
//             };

//             return Result<GetOutboxMessageByIdResponse>.Success(response);
//         }
//         catch (PersistenceException exception)
//         {
//             return Result<GetOutboxMessageByIdResponse>.Failure(
//                 MapPersistenceException(exception));
//         }
//     }

//     private static Error MapPersistenceException(PersistenceException exception)
//     {
//         return exception.Code switch
//         {
//             "NOTIFICATIONS.OUTBOX_MESSAGE_NOT_FOUND" => NotificationsErrors.OutboxMessage.NotFound,
//             _ => NotificationsErrors.ValidationFailed
//         };
//     }
// }