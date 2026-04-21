// using System.Text.Json;
// using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
// using CommercialNews.BuildingBlocks.SharedKernel.Results;
// using CommercialNews.BuildingBlocks.SharedKernel.Time;
// using Notifications.Application.Contracts.Outbox.Requests;
// using Notifications.Application.Contracts.Outbox.Responses;
// using Notifications.Application.Contracts.Processing.Requests;
// using Notifications.Application.Contracts.Processing.Responses;
// using Notifications.Application.Errors;
// using Notifications.Application.Ports.Persistence.Transactions;
// using Notifications.Application.Ports.Persistence.Write;
// using Notifications.Application.UseCases.Processing.ProcessEmailDelivery;
// using Notifications.Domain.Entities;
// using Notifications.Domain.Enums;
// using Notifications.Domain.Exceptions;

// namespace Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;

// /// <summary>
// /// Processes a single durable outbox message inside the notification module.
// /// This use case parses the outbox payload, finds or creates the corresponding email delivery,
// /// delegates actual email execution to ProcessEmailDeliveryUseCase, and then updates the outbox state safely.
// /// It does not make upstream business truth valid; it only advances notification runtime state.
// /// </summary>
// public sealed class ProcessOutboxMessageUseCase : IProcessOutboxMessageUseCase
// {
//     private readonly IOutboxMessageRepository _outboxMessageRepository;
//     private readonly IEmailDeliveryRepository _emailDeliveryRepository;
//     private readonly INotificationsUnitOfWork _unitOfWork;
//     private readonly IDateTimeProvider _dateTimeProvider;
//     private readonly IProcessEmailDeliveryUseCase _processEmailDeliveryUseCase;

//     public ProcessOutboxMessageUseCase(
//         IOutboxMessageRepository outboxMessageRepository,
//         IEmailDeliveryRepository emailDeliveryRepository,
//         INotificationsUnitOfWork unitOfWork,
//         IDateTimeProvider dateTimeProvider,
//         IProcessEmailDeliveryUseCase processEmailDeliveryUseCase)
//     {
//         _outboxMessageRepository = outboxMessageRepository
//             ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
//         _emailDeliveryRepository = emailDeliveryRepository
//             ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
//         _unitOfWork = unitOfWork
//             ?? throw new ArgumentNullException(nameof(unitOfWork));
//         _dateTimeProvider = dateTimeProvider
//             ?? throw new ArgumentNullException(nameof(dateTimeProvider));
//         _processEmailDeliveryUseCase = processEmailDeliveryUseCase
//             ?? throw new ArgumentNullException(nameof(processEmailDeliveryUseCase));
//     }

//     public async Task<Result<ProcessOutboxMessageResponse>> ExecuteAsync(
//         ProcessOutboxMessageRequest request,
//         CancellationToken cancellationToken = default)
//     {
//         try
//         {
//             if (request.OutboxMessageId <= 0)
//             {
//                 return Result<ProcessOutboxMessageResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.InvalidId);
//             }

//             OutboxMessage? outboxMessage = await _outboxMessageRepository.GetByIdAsync(
//                 request.OutboxMessageId,
//                 cancellationToken);

//             if (outboxMessage is null)
//             {
//                 return Result<ProcessOutboxMessageResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.NotFound);
//             }

//             if (string.Equals(outboxMessage.Status, OutboxMessageStatus.Published, StringComparison.OrdinalIgnoreCase))
//             {
//                 return Result<ProcessOutboxMessageResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.AlreadyPublished);
//             }

//             if (string.Equals(outboxMessage.Status, OutboxMessageStatus.DeadLetter, StringComparison.OrdinalIgnoreCase))
//             {
//                 return Result<ProcessOutboxMessageResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.DeadLettered);
//             }

//             OutboxPayloadData payloadData = ParsePayload(outboxMessage.Payload);

//             EmailDelivery? emailDelivery = await _emailDeliveryRepository.GetByMessageIdAsync(
//                 outboxMessage.MessageId,
//                 cancellationToken);

//             if (emailDelivery is null)
//             {
//                 emailDelivery = await _emailDeliveryRepository.GetByBusinessDedupeKeyAsync(
//                     payloadData.BusinessDedupeKey,
//                     cancellationToken);
//             }

//             if (emailDelivery is null)
//             {
//                 DateTime nowUtc = _dateTimeProvider.UtcNow;

//                 EmailDelivery newEmailDelivery = EmailDelivery.Create(
//                     messageId: outboxMessage.MessageId,
//                     businessDedupeKey: payloadData.BusinessDedupeKey,
//                     toEmail: payloadData.ToEmail,
//                     templateKey: payloadData.TemplateKey,
//                     provider: payloadData.Provider,
//                     nowUtc: nowUtc,
//                     recipientUserId: payloadData.RecipientUserId,
//                     toEmailHash: payloadData.ToEmailHash,
//                     templateVersion: payloadData.TemplateVersion,
//                     subject: payloadData.Subject,
//                     correlationId: payloadData.CorrelationId ?? outboxMessage.CorrelationId);

//                 await _unitOfWork.BeginTransactionAsync(cancellationToken);

//                 try
//                 {
//                     long emailDeliveryId = await _emailDeliveryRepository.InsertAsync(
//                         newEmailDelivery,
//                         cancellationToken);

//                     await _unitOfWork.CommitAsync(cancellationToken);

//                     emailDelivery = await _emailDeliveryRepository.GetByIdAsync(
//                         emailDeliveryId,
//                         cancellationToken);
//                 }
//                 catch
//                 {
//                     await _unitOfWork.RollbackAsync(cancellationToken);
//                     throw;
//                 }

//                 if (emailDelivery is null)
//                 {
//                     return Result<ProcessOutboxMessageResponse>.Failure(
//                         NotificationsErrors.EmailDelivery.NotFound);
//                 }
//             }

//             if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Sent, StringComparison.OrdinalIgnoreCase) ||
//                 string.Equals(emailDelivery.Status, EmailDeliveryStatus.Suppressed, StringComparison.OrdinalIgnoreCase))
//             {
//                 return await MarkOutboxPublishedAsync(
//                     outboxMessage,
//                     emailDelivery,
//                     cancellationToken);
//             }

//             Result<ProcessEmailDeliveryResponse> processingResult =
//                 await _processEmailDeliveryUseCase.ExecuteAsync(
//                     new ProcessEmailDeliveryRequest
//                     {
//                         EmailDeliveryId = emailDelivery.EmailDeliveryId,
//                         Variables = payloadData.Variables,
//                         CorrelationId = payloadData.CorrelationId ?? outboxMessage.CorrelationId
//                     },
//                     cancellationToken);

//             if (processingResult.IsFailure)
//             {
//                 Error error = processingResult.Error
//                     ?? NotificationsErrors.ValidationFailed;

//                 return await HandleProcessingFailureAsync(
//                     outboxMessage,
//                     emailDelivery,
//                     error,
//                     cancellationToken);
//             }

//             ProcessEmailDeliveryResponse processingResponse = processingResult.Value
//                 ?? throw new InvalidOperationException(
//                     "ProcessEmailDeliveryUseCase returned success without a response value.");

//             return await MarkOutboxPublishedAsync(
//                 outboxMessage,
//                 emailDelivery,
//                 cancellationToken,
//                 processingResponse.Status);

//         }
//         catch (PersistenceException exception)
//         {
//             return Result<ProcessOutboxMessageResponse>.Failure(
//                 MapPersistenceException(exception));
//         }
//         catch (NotificationsDomainException exception)
//         {
//             return Result<ProcessOutboxMessageResponse>.Failure(
//                 MapDomainException(exception));
//         }
//         catch (JsonException)
//         {
//             return Result<ProcessOutboxMessageResponse>.Failure(
//                 NotificationsErrors.OutboxMessage.PayloadInvalid);
//         }
//     }

//     private async Task<Result<ProcessOutboxMessageResponse>> HandleProcessingFailureAsync(
//         OutboxMessage outboxMessage,
//         EmailDelivery emailDelivery,
//         Error error,
//         CancellationToken cancellationToken)
//     {
//         bool shouldDeadLetter = error.Code switch
//         {
//             "NOTIFICATIONS.TEMPLATE_KEY_INVALID" => true,
//             "NOTIFICATIONS.TEMPLATE_RENDER_FAILED" => true,
//             "NOTIFICATIONS.UNSAFE_TEMPLATE_VARIABLES" => true,
//             "NOTIFICATIONS.EMAIL_DELIVERY_ALREADY_SENT" => false,
//             "NOTIFICATIONS.EMAIL_DELIVERY_ALREADY_SUPPRESSED" => false,
//             _ => false
//         };

//         await _unitOfWork.BeginTransactionAsync(cancellationToken);

//         try
//         {
//             int affectedRows;

//             if (shouldDeadLetter)
//             {
//                 affectedRows = await _outboxMessageRepository.MarkDeadLetterAsync(
//                     outboxMessage.OutboxMessageId,
//                     error.Message,
//                     error.Code,
//                     EmailErrorClass.Validation,
//                     cancellationToken);
//             }
//             else
//             {
//                 DateTime nowUtc = _dateTimeProvider.UtcNow;

//                 affectedRows = await _outboxMessageRepository.MarkFailedAsync(
//                     outboxMessage.OutboxMessageId,
//                     nowUtc.AddMinutes(1),
//                     error.Message,
//                     error.Code,
//                     EmailErrorClass.Transient,
//                     cancellationToken);
//             }

//             if (affectedRows <= 0)
//             {
//                 await _unitOfWork.RollbackAsync(cancellationToken);

//                 return Result<ProcessOutboxMessageResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.InvalidStateTransition);
//             }

//             await _unitOfWork.CommitAsync(cancellationToken);

//             return Result<ProcessOutboxMessageResponse>.Success(
//                 new ProcessOutboxMessageResponse
//                 {
//                     OutboxMessageId = outboxMessage.OutboxMessageId,
//                     MessageId = outboxMessage.MessageId,
//                     EmailDeliveryId = emailDelivery.EmailDeliveryId,
//                     OutboxStatus = shouldDeadLetter
//                         ? OutboxMessageStatus.DeadLetter
//                         : OutboxMessageStatus.Failed,
//                     EmailDeliveryStatus = emailDelivery.Status,
//                     Processed = false
//                 });
//         }
//         catch
//         {
//             await _unitOfWork.RollbackAsync(cancellationToken);
//             throw;
//         }
//     }

//     private async Task<Result<ProcessOutboxMessageResponse>> MarkOutboxPublishedAsync(
//         OutboxMessage outboxMessage,
//         EmailDelivery emailDelivery,
//         CancellationToken cancellationToken,
//         string? emailDeliveryStatusOverride = null)
//     {
//         await _unitOfWork.BeginTransactionAsync(cancellationToken);

//         try
//         {
//             int affectedRows = await _outboxMessageRepository.MarkPublishedAsync(
//                 outboxMessage.OutboxMessageId,
//                 cancellationToken);

//             if (affectedRows <= 0)
//             {
//                 await _unitOfWork.RollbackAsync(cancellationToken);

//                 return Result<ProcessOutboxMessageResponse>.Failure(
//                     NotificationsErrors.OutboxMessage.InvalidStateTransition);
//             }

//             await _unitOfWork.CommitAsync(cancellationToken);

//             return Result<ProcessOutboxMessageResponse>.Success(
//                 new ProcessOutboxMessageResponse
//                 {
//                     OutboxMessageId = outboxMessage.OutboxMessageId,
//                     MessageId = outboxMessage.MessageId,
//                     EmailDeliveryId = emailDelivery.EmailDeliveryId,
//                     OutboxStatus = OutboxMessageStatus.Published,
//                     EmailDeliveryStatus = emailDeliveryStatusOverride ?? emailDelivery.Status,
//                     Processed = true
//                 });
//         }
//         catch
//         {
//             await _unitOfWork.RollbackAsync(cancellationToken);
//             throw;
//         }
//     }

//     private static OutboxPayloadData ParsePayload(string payload)
//     {
//         using JsonDocument document = JsonDocument.Parse(payload);
//         JsonElement root = document.RootElement;

//         string businessDedupeKey = GetRequiredString(root, "businessDedupeKey");
//         string toEmail = GetRequiredString(root, "toEmail");
//         string templateKey = GetRequiredString(root, "templateKey");

//         long? recipientUserId = TryGetInt64(root, "recipientUserId");
//         string? toEmailHash = TryGetString(root, "toEmailHash");
//         int? templateVersion = TryGetInt32(root, "templateVersion");
//         string? subject = TryGetString(root, "subject");
//         string provider = TryGetString(root, "provider") ?? "smtp";
//         string? correlationId = TryGetString(root, "correlationId");

//         Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);

//         if (root.TryGetProperty("variables", out JsonElement variablesElement) &&
//             variablesElement.ValueKind == JsonValueKind.Object)
//         {
//             foreach (JsonProperty property in variablesElement.EnumerateObject())
//             {
//                 variables[property.Name] = property.Value.ValueKind switch
//                 {
//                     JsonValueKind.String => property.Value.GetString() ?? string.Empty,
//                     JsonValueKind.Number => property.Value.GetRawText(),
//                     JsonValueKind.True => "true",
//                     JsonValueKind.False => "false",
//                     _ => property.Value.GetRawText()
//                 };
//             }
//         }

//         return new OutboxPayloadData
//         {
//             BusinessDedupeKey = businessDedupeKey,
//             RecipientUserId = recipientUserId,
//             ToEmail = toEmail,
//             ToEmailHash = toEmailHash,
//             TemplateKey = templateKey,
//             TemplateVersion = templateVersion,
//             Subject = subject,
//             Provider = provider,
//             CorrelationId = correlationId,
//             Variables = variables
//         };
//     }

//     private static string GetRequiredString(JsonElement root, string propertyName)
//     {
//         if (!root.TryGetProperty(propertyName, out JsonElement property) ||
//             property.ValueKind != JsonValueKind.String ||
//             string.IsNullOrWhiteSpace(property.GetString()))
//         {
//             throw new NotificationsDomainException(
//                 "NOTIFICATIONS.OUTBOX_PAYLOAD_INVALID",
//                 $"Outbox payload field '{propertyName}' is required.");
//         }

//         return property.GetString()!.Trim();
//     }

//     private static string? TryGetString(JsonElement root, string propertyName)
//     {
//         if (!root.TryGetProperty(propertyName, out JsonElement property) ||
//             property.ValueKind == JsonValueKind.Null ||
//             property.ValueKind == JsonValueKind.Undefined)
//         {
//             return null;
//         }

//         if (property.ValueKind == JsonValueKind.String)
//         {
//             string? value = property.GetString();

//             return string.IsNullOrWhiteSpace(value)
//                 ? null
//                 : value.Trim();
//         }

//         return property.GetRawText();
//     }

//     private static long? TryGetInt64(JsonElement root, string propertyName)
//     {
//         if (!root.TryGetProperty(propertyName, out JsonElement property))
//         {
//             return null;
//         }

//         if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out long value))
//         {
//             return value;
//         }

//         return null;
//     }

//     private static int? TryGetInt32(JsonElement root, string propertyName)
//     {
//         if (!root.TryGetProperty(propertyName, out JsonElement property))
//         {
//             return null;
//         }

//         if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int value))
//         {
//             return value;
//         }

//         return null;
//     }

//     private static Error MapDomainException(NotificationsDomainException exception)
//     {
//         return exception.Code switch
//         {
//             "NOTIFICATIONS.OUTBOX_INVALID_ID" => NotificationsErrors.OutboxMessage.InvalidId,
//             "NOTIFICATIONS.OUTBOX_MESSAGE_ID_REQUIRED" => NotificationsErrors.OutboxMessage.MessageIdRequired,
//             "NOTIFICATIONS.OUTBOX_INVALID_STATE_TRANSITION" => NotificationsErrors.OutboxMessage.InvalidStateTransition,
//             "NOTIFICATIONS.OUTBOX_PAYLOAD_INVALID" => NotificationsErrors.OutboxMessage.PayloadInvalid,
//             "NOTIFICATIONS.EMAIL_DELIVERY_TEMPLATE_KEY_INVALID" => NotificationsErrors.EmailDelivery.TemplateKeyInvalid,
//             _ => NotificationsErrors.ValidationFailed
//         };
//     }

//     private static Error MapPersistenceException(PersistenceException exception)
//     {
//         return exception.Code switch
//         {
//             "NOTIFICATIONS.OUTBOX_MESSAGE_NOT_FOUND" => NotificationsErrors.OutboxMessage.NotFound,
//             "NOTIFICATIONS.OUTBOX_MESSAGE_ALREADY_PUBLISHED" => NotificationsErrors.OutboxMessage.AlreadyPublished,
//             "NOTIFICATIONS.OUTBOX_STALE_WRITE_CONFLICT" => NotificationsErrors.OutboxMessage.StaleWriteConflict,
//             "NOTIFICATIONS.EMAIL_DELIVERY_NOT_FOUND" => NotificationsErrors.EmailDelivery.NotFound,
//             "NOTIFICATIONS.EMAIL_DELIVERY_DUPLICATE_BUSINESS_INTENT" => NotificationsErrors.EmailDelivery.DuplicateBusinessIntent,
//             _ => NotificationsErrors.ValidationFailed
//         };
//     }

//     private sealed class OutboxPayloadData
//     {
//         public string BusinessDedupeKey { get; init; } = string.Empty;

//         public long? RecipientUserId { get; init; }

//         public string ToEmail { get; init; } = string.Empty;

//         public string? ToEmailHash { get; init; }

//         public string TemplateKey { get; init; } = string.Empty;

//         public int? TemplateVersion { get; init; }

//         public string? Subject { get; init; }

//         public string Provider { get; init; } = "smtp";

//         public string? CorrelationId { get; init; }

//         public IReadOnlyDictionary<string, string> Variables { get; init; }
//             = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
//     }
// }