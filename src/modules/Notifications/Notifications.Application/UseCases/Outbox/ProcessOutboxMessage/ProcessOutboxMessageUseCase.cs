using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Models.OutboxPayloads;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Transactions;
using Notifications.Application.Validation.Outbox.ProcessOutboxMessage;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;

namespace Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;

/// <summary>
/// Phase note:
/// This use case currently translates known outbox events into Notifications-owned
/// EmailDelivery records. It is intentionally scoped to Notifications because the
/// active downstream action today is email delivery creation.
///
/// Future evolution:
/// If outbox processing becomes a shared runtime concern across multiple modules,
/// move the orchestration responsibility into building-blocks and keep only
/// module-specific event handlers inside each module.
/// </summary>
public sealed class ProcessOutboxMessageUseCase : IProcessOutboxMessageUseCase
{
    private const string VerificationEmailRequestedEventType =
        "Identity.VerificationEmailRequested";

    private const string PasswordChangedEmailRequestedEventType =
        "Identity.PasswordChangedEmailRequested";

    private const string PasswordResetRequestedEventType =
        "Identity.PasswordResetRequested";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProcessOutboxMessageUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        IEmailDeliveryRepository emailDeliveryRepository,
        INotificationsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<ProcessOutboxMessageResponse>> ExecuteAsync(
        ProcessOutboxMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ProcessOutboxMessageValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ProcessOutboxMessageResponse>.Failure(validationError);
        }

        try
        {
            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<ProcessOutboxMessageResponse>.Failure(
                    NotificationsErrors.Outbox.NotFound);
            }

            if (!string.Equals(outboxMessage.Status, OutboxMessageStatus.Pending, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(outboxMessage.Status, OutboxMessageStatus.Failed, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(outboxMessage.Status, OutboxMessageStatus.Publishing, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ProcessOutboxMessageResponse>.Failure(
                    NotificationsErrors.Outbox.InvalidState);
            }

            return outboxMessage.EventType switch
            {
                VerificationEmailRequestedEventType => await ProcessIdentityEmailEventAsync(
                    outboxMessage,
                    cancellationToken),

                PasswordChangedEmailRequestedEventType => await ProcessIdentityEmailEventAsync(
                    outboxMessage,
                    cancellationToken),

                PasswordResetRequestedEventType => await ProcessIdentityEmailEventAsync(
                    outboxMessage,
                    cancellationToken),

                _ => Result<ProcessOutboxMessageResponse>.Failure(
                    NotificationsErrors.Outbox.UnsupportedEventType)
            };
        }
        catch (PersistenceException exception)
        {
            return Result<ProcessOutboxMessageResponse>.Failure(
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

    private async Task<Result<ProcessOutboxMessageResponse>> ProcessIdentityEmailEventAsync(
        CommercialNews.BuildingBlocks.Outbox.Models.OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        IdentityVerificationEmailRequestedPayload? payload;

        try
        {
            payload = JsonSerializer.Deserialize<IdentityVerificationEmailRequestedPayload>(
                outboxMessage.Payload,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return Result<ProcessOutboxMessageResponse>.Failure(
                NotificationsErrors.Outbox.PayloadInvalid);
        }

        if (payload is null)
        {
            return Result<ProcessOutboxMessageResponse>.Failure(
                NotificationsErrors.Outbox.PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(payload.BusinessDedupeKey) ||
            payload.RecipientUserId <= 0 ||
            string.IsNullOrWhiteSpace(payload.ToEmail) ||
            string.IsNullOrWhiteSpace(payload.TemplateKey) ||
            string.IsNullOrWhiteSpace(payload.Provider) ||
            payload.Variables is null ||
            payload.Variables.IsEmpty())
        {
            return Result<ProcessOutboxMessageResponse>.Failure(
                NotificationsErrors.Outbox.PayloadInvalid);
        }

        var existingDelivery = await _emailDeliveryRepository.GetByBusinessDedupeKeyAsync(
            payload.BusinessDedupeKey,
            cancellationToken);

        if (existingDelivery is not null)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _outboxMessageRepository.MarkPublishedAsync(
                    _unitOfWork,
                    outboxMessage.OutboxMessageId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return Result<ProcessOutboxMessageResponse>.Success(
                new ProcessOutboxMessageResponse
                {
                    OutboxMessageId = outboxMessage.OutboxMessageId,
                    MessageId = outboxMessage.MessageId,
                    EventType = outboxMessage.EventType,
                    CreatedEmailDelivery = false,
                    EmailDeliveryId = existingDelivery.EmailDeliveryId,
                    Status = OutboxMessageStatus.Published
                });
        }

        DateTime nowUtc = _dateTimeProvider.UtcNow;
        string variablesJson = JsonSerializer.Serialize(payload.Variables, SerializerOptions);

        EmailDelivery emailDelivery = EmailDelivery.Create(
            messageId: outboxMessage.MessageId,
            businessDedupeKey: payload.BusinessDedupeKey,
            toEmail: payload.ToEmail.Trim(),
            templateKey: payload.TemplateKey.Trim(),
            variablesJson: variablesJson,
            provider: payload.Provider.Trim(),
            priority: 5,
            nowUtc: nowUtc,
            recipientUserId: payload.RecipientUserId,
            correlationId: string.IsNullOrWhiteSpace(payload.CorrelationId)
                ? outboxMessage.CorrelationId
                : payload.CorrelationId.Trim());

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            long emailDeliveryId = await _emailDeliveryRepository.InsertAsync(
                emailDelivery,
                cancellationToken);

            await _outboxMessageRepository.MarkPublishedAsync(
                _unitOfWork,
                outboxMessage.OutboxMessageId,
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProcessOutboxMessageResponse>.Success(
                new ProcessOutboxMessageResponse
                {
                    OutboxMessageId = outboxMessage.OutboxMessageId,
                    MessageId = outboxMessage.MessageId,
                    EventType = outboxMessage.EventType,
                    CreatedEmailDelivery = true,
                    EmailDeliveryId = emailDeliveryId,
                    Status = OutboxMessageStatus.Published
                });
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}