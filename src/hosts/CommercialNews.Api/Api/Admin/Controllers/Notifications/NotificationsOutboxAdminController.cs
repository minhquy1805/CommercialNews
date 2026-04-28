// using CommercialNews.Api.Api.Admin.Contracts.Notifications.Outbox.Requests;
// using CommercialNews.Api.Api.Admin.Contracts.Notifications.Outbox.Responses;
// using CommercialNews.Api.Api.Common.ErrorHandling;
// using CommercialNews.Api.Api.ErrorHandling;
// using CommercialNews.BuildingBlocks.Outbox.UseCases.GetOutboxMessageById;
// using CommercialNews.BuildingBlocks.Outbox.UseCases.GetOutboxMessageByMessageId;
// using CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxDead;
// using CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxFailed;
// using CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxPublished;
// using CommercialNews.BuildingBlocks.SharedKernel.Results;
// using Microsoft.AspNetCore.Mvc;
// using Notifications.Application.Contracts.Outbox.Requests;
// using Notifications.Application.Contracts.Outbox.Responses;

// namespace CommercialNews.Api.Api.Admin.Controllers.Notifications;

// [ApiController]
// [Route("api/v1/admin/notifications/outbox")]
// public sealed class NotificationsOutboxAdminController : ControllerBase
// {
//     private readonly IGetOutboxMessageByIdUseCase _getOutboxMessageByIdUseCase;
//     private readonly IGetOutboxMessageByMessageIdUseCase _getOutboxMessageByMessageIdUseCase;
//     private readonly IMarkOutboxPublishedUseCase _markOutboxPublishedUseCase;
//     private readonly IMarkOutboxFailedUseCase _markOutboxFailedUseCase;
//     private readonly IMarkOutboxDeadUseCase _markOutboxDeadUseCase;

//     public NotificationsOutboxAdminController(
//         IGetOutboxMessageByIdUseCase getOutboxMessageByIdUseCase,
//         IGetOutboxMessageByMessageIdUseCase getOutboxMessageByMessageIdUseCase,
//         IMarkOutboxPublishedUseCase markOutboxPublishedUseCase,
//         IMarkOutboxFailedUseCase markOutboxFailedUseCase,
//         IMarkOutboxDeadUseCase markOutboxDeadUseCase)
//     {
//         _getOutboxMessageByIdUseCase = getOutboxMessageByIdUseCase
//             ?? throw new ArgumentNullException(nameof(getOutboxMessageByIdUseCase));
//         _getOutboxMessageByMessageIdUseCase = getOutboxMessageByMessageIdUseCase
//             ?? throw new ArgumentNullException(nameof(getOutboxMessageByMessageIdUseCase));
//         _markOutboxPublishedUseCase = markOutboxPublishedUseCase
//             ?? throw new ArgumentNullException(nameof(markOutboxPublishedUseCase));
//         _markOutboxFailedUseCase = markOutboxFailedUseCase
//             ?? throw new ArgumentNullException(nameof(markOutboxFailedUseCase));
//         _markOutboxDeadUseCase = markOutboxDeadUseCase
//             ?? throw new ArgumentNullException(nameof(markOutboxDeadUseCase));
//     }

//     [HttpGet("{outboxMessageId:long}")]
//     [ProducesResponseType(typeof(GetOutboxMessageByIdHttpResponse), StatusCodes.Status200OK)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
//     public async Task<IActionResult> GetOutboxMessageByIdAsync(
//         [FromRoute] long outboxMessageId,
//         CancellationToken cancellationToken)
//     {
//         Result<GetOutboxMessageByIdResponse> result =
//             await _getOutboxMessageByIdUseCase.ExecuteAsync(
//                 new GetOutboxMessageByIdRequest
//                 {
//                     OutboxMessageId = outboxMessageId
//                 },
//                 cancellationToken);

//         if (result.IsFailure)
//         {
//             return this.ToActionResult(
//                 Result<GetOutboxMessageByIdHttpResponse>.Failure(result.Error!));
//         }

//         GetOutboxMessageByIdHttpResponse response = MapOutboxDetailResponse(result.Value!);

//         return this.ToActionResult(
//             Result<GetOutboxMessageByIdHttpResponse>.Success(response));
//     }

//     [HttpGet("by-message/{messageId}")]
//     [ProducesResponseType(typeof(GetOutboxMessageByIdHttpResponse), StatusCodes.Status200OK)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
//     public async Task<IActionResult> GetOutboxMessageByMessageIdAsync(
//         [FromRoute] string messageId,
//         CancellationToken cancellationToken)
//     {
//         Result<GetOutboxMessageByIdResponse> result =
//             await _getOutboxMessageByMessageIdUseCase.ExecuteAsync(
//                 new GetOutboxMessageByMessageIdRequest
//                 {
//                     MessageId = messageId
//                 },
//                 cancellationToken);

//         if (result.IsFailure)
//         {
//             return this.ToActionResult(
//                 Result<GetOutboxMessageByIdHttpResponse>.Failure(result.Error!));
//         }

//         GetOutboxMessageByIdHttpResponse response = MapOutboxDetailResponse(result.Value!);

//         return this.ToActionResult(
//             Result<GetOutboxMessageByIdHttpResponse>.Success(response));
//     }

//     [HttpPost("{outboxMessageId:long}:mark-published")]
//     [ProducesResponseType(typeof(MarkOutboxPublishedHttpResponse), StatusCodes.Status200OK)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
//     public async Task<IActionResult> MarkOutboxPublishedAsync(
//         [FromRoute] long outboxMessageId,
//         CancellationToken cancellationToken)
//     {
//         Result<MarkOutboxPublishedResponse> result =
//             await _markOutboxPublishedUseCase.ExecuteAsync(
//                 new MarkOutboxPublishedRequest
//                 {
//                     OutboxMessageId = outboxMessageId
//                 },
//                 cancellationToken);

//         if (result.IsFailure)
//         {
//             return this.ToActionResult(
//                 Result<MarkOutboxPublishedHttpResponse>.Failure(result.Error!));
//         }

//         MarkOutboxPublishedHttpResponse response = new()
//         {
//             OutboxMessageId = result.Value!.OutboxMessageId,
//             MessageId = result.Value.MessageId,
//             Status = result.Value.Status
//         };

//         return this.ToActionResult(
//             Result<MarkOutboxPublishedHttpResponse>.Success(response));
//     }

//     [HttpPost("{outboxMessageId:long}:mark-failed")]
//     [ProducesResponseType(typeof(MarkOutboxFailedHttpResponse), StatusCodes.Status200OK)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
//     public async Task<IActionResult> MarkOutboxFailedAsync(
//         [FromRoute] long outboxMessageId,
//         [FromBody] MarkOutboxFailedHttpRequest request,
//         CancellationToken cancellationToken)
//     {
//         Result<MarkOutboxFailedResponse> result =
//             await _markOutboxFailedUseCase.ExecuteAsync(
//                 new MarkOutboxFailedRequest
//                 {
//                     OutboxMessageId = outboxMessageId,
//                     NextRetryAt = request.NextRetryAt,
//                     LastError = request.LastError,
//                     LastErrorCode = request.LastErrorCode,
//                     LastErrorClass = request.LastErrorClass
//                 },
//                 cancellationToken);

//         if (result.IsFailure)
//         {
//             return this.ToActionResult(
//                 Result<MarkOutboxFailedHttpResponse>.Failure(result.Error!));
//         }

//         MarkOutboxFailedHttpResponse response = new()
//         {
//             OutboxMessageId = result.Value!.OutboxMessageId,
//             MessageId = result.Value.MessageId,
//             Status = result.Value.Status,
//             NextRetryAt = result.Value.NextRetryAt
//         };

//         return this.ToActionResult(
//             Result<MarkOutboxFailedHttpResponse>.Success(response));
//     }

//     [HttpPost("{outboxMessageId:long}:mark-dead")]
//     [ProducesResponseType(typeof(MarkOutboxDeadHttpResponse), StatusCodes.Status200OK)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
//     [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
//     public async Task<IActionResult> MarkOutboxDeadAsync(
//         [FromRoute] long outboxMessageId,
//         [FromBody] MarkOutboxDeadHttpRequest request,
//         CancellationToken cancellationToken)
//     {
//         Result<MarkOutboxDeadResponse> result =
//             await _markOutboxDeadUseCase.ExecuteAsync(
//                 new MarkOutboxDeadRequest
//                 {
//                     OutboxMessageId = outboxMessageId,
//                     LastError = request.LastError,
//                     LastErrorCode = request.LastErrorCode,
//                     LastErrorClass = request.LastErrorClass
//                 },
//                 cancellationToken);

//         if (result.IsFailure)
//         {
//             return this.ToActionResult(
//                 Result<MarkOutboxDeadHttpResponse>.Failure(result.Error!));
//         }

//         MarkOutboxDeadHttpResponse response = new()
//         {
//             OutboxMessageId = result.Value!.OutboxMessageId,
//             MessageId = result.Value.MessageId,
//             Status = result.Value.Status
//         };

//         return this.ToActionResult(
//             Result<MarkOutboxDeadHttpResponse>.Success(response));
//     }

//     private static GetOutboxMessageByIdHttpResponse MapOutboxDetailResponse(
//         GetOutboxMessageByIdResponse value)
//     {
//         return new GetOutboxMessageByIdHttpResponse
//         {
//             OutboxMessageId = value.OutboxMessageId,
//             MessageId = value.MessageId,
//             EventType = value.EventType,
//             AggregateType = value.AggregateType,
//             AggregateId = value.AggregateId,
//             AggregatePublicId = value.AggregatePublicId,
//             AggregateVersion = value.AggregateVersion,
//             Payload = value.Payload,
//             Headers = value.Headers,
//             CorrelationId = value.CorrelationId,
//             InitiatorUserId = value.InitiatorUserId,
//             Priority = value.Priority,
//             Status = value.Status,
//             AttemptCount = value.AttemptCount,
//             NextRetryAt = value.NextRetryAt,
//             LastAttemptAt = value.LastAttemptAt,
//             PublishedAt = value.PublishedAt,
//             LastError = value.LastError,
//             LastErrorCode = value.LastErrorCode,
//             LastErrorClass = value.LastErrorClass,
//             OccurredAt = value.OccurredAt,
//             CreatedAt = value.CreatedAt,
//             UpdatedAt = value.UpdatedAt
//         };
//     }
// }