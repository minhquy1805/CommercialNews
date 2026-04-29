using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using Notifications.Application.Outbox;
using Notifications.Application.Outbox.Payloads;
using Notifications.Application.Ports.Services;
using Notifications.Application.Ports.Transactions;

namespace Notifications.Infrastructure.Services;

public sealed class NotificationsOutboxWriter : INotificationsOutboxWriter
{
    private const string AggregateTypeEmailDelivery = "EmailDelivery";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public NotificationsOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public Task<long> EnqueueEmailSentAsync(
        INotificationsUnitOfWork unitOfWork,
        long emailDeliveryId,
        long emailDeliveryAttemptId,
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string templateKey,
        string provider,
        int attemptCount,
        string? providerMessageId,
        string? correlationId,
        DateTime sentAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateDeliveryEnvelope(
            emailDeliveryId,
            emailDeliveryAttemptId,
            messageId,
            businessDedupeKey,
            toEmail,
            templateKey,
            provider,
            attemptCount,
            sentAtUtc);

        var payload = new EmailSentIntegrationEventPayload(
            EmailDeliveryId: emailDeliveryId,
            EmailDeliveryAttemptId: emailDeliveryAttemptId,
            MessageId: messageId.Trim(),
            BusinessDedupeKey: businessDedupeKey.Trim(),
            RecipientUserId: recipientUserId,
            ToEmail: toEmail.Trim(),
            TemplateKey: templateKey.Trim(),
            Provider: provider.Trim(),
            AttemptCount: attemptCount,
            ProviderMessageId: NormalizeOptional(providerMessageId),
            CorrelationId: NormalizeOptional(correlationId),
            SentAtUtc: sentAtUtc);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: NotificationsIntegrationEventTypes.EmailSent,
            emailDeliveryId: emailDeliveryId,
            payload: payload,
            occurredAtUtc: sentAtUtc,
            priority: 5,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueEmailFailedAsync(
        INotificationsUnitOfWork unitOfWork,
        long emailDeliveryId,
        long emailDeliveryAttemptId,
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string templateKey,
        string provider,
        int attemptCount,
        DateTime? nextRetryAtUtc,
        string? lastErrorCode,
        string? lastErrorClass,
        bool isAmbiguous,
        string? correlationId,
        DateTime failedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateDeliveryEnvelope(
            emailDeliveryId,
            emailDeliveryAttemptId,
            messageId,
            businessDedupeKey,
            toEmail,
            templateKey,
            provider,
            attemptCount,
            failedAtUtc);

        var payload = new EmailFailedIntegrationEventPayload(
            EmailDeliveryId: emailDeliveryId,
            EmailDeliveryAttemptId: emailDeliveryAttemptId,
            MessageId: messageId.Trim(),
            BusinessDedupeKey: businessDedupeKey.Trim(),
            RecipientUserId: recipientUserId,
            ToEmail: toEmail.Trim(),
            TemplateKey: templateKey.Trim(),
            Provider: provider.Trim(),
            AttemptCount: attemptCount,
            NextRetryAtUtc: nextRetryAtUtc,
            LastErrorCode: NormalizeOptional(lastErrorCode),
            LastErrorClass: NormalizeOptional(lastErrorClass),
            IsAmbiguous: isAmbiguous,
            CorrelationId: NormalizeOptional(correlationId),
            FailedAtUtc: failedAtUtc);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: NotificationsIntegrationEventTypes.EmailFailed,
            emailDeliveryId: emailDeliveryId,
            payload: payload,
            occurredAtUtc: failedAtUtc,
            priority: 3,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueEmailDeadAsync(
        INotificationsUnitOfWork unitOfWork,
        long emailDeliveryId,
        long emailDeliveryAttemptId,
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string templateKey,
        string provider,
        int attemptCount,
        string? lastErrorCode,
        string? lastErrorClass,
        bool isAmbiguous,
        string? correlationId,
        DateTime deadAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateDeliveryEnvelope(
            emailDeliveryId,
            emailDeliveryAttemptId,
            messageId,
            businessDedupeKey,
            toEmail,
            templateKey,
            provider,
            attemptCount,
            deadAtUtc);

        var payload = new EmailDeadIntegrationEventPayload(
            EmailDeliveryId: emailDeliveryId,
            EmailDeliveryAttemptId: emailDeliveryAttemptId,
            MessageId: messageId.Trim(),
            BusinessDedupeKey: businessDedupeKey.Trim(),
            RecipientUserId: recipientUserId,
            ToEmail: toEmail.Trim(),
            TemplateKey: templateKey.Trim(),
            Provider: provider.Trim(),
            AttemptCount: attemptCount,
            LastErrorCode: NormalizeOptional(lastErrorCode),
            LastErrorClass: NormalizeOptional(lastErrorClass),
            IsAmbiguous: isAmbiguous,
            CorrelationId: NormalizeOptional(correlationId),
            DeadAtUtc: deadAtUtc);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: NotificationsIntegrationEventTypes.EmailDead,
            emailDeliveryId: emailDeliveryId,
            payload: payload,
            occurredAtUtc: deadAtUtc,
            priority: 1,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    private async Task<long> InsertOutboxMessageAsync<TPayload>(
        INotificationsUnitOfWork unitOfWork,
        string eventType,
        long emailDeliveryId,
        TPayload payload,
        DateTime occurredAtUtc,
        byte priority,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        string payloadJson = JsonSerializer.Serialize(
            payload,
            SerializerOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: eventType,
            aggregateType: AggregateTypeEmailDelivery,
            aggregateId: emailDeliveryId.ToString(),
            payload: payloadJson,
            occurredAt: occurredAtUtc,
            priority: priority,
            aggregatePublicId: null,
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeOptional(correlationId),
            initiatorUserId: null);

        return await _outboxMessageRepository.InsertAsync(
            unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static void ValidateDeliveryEnvelope(
        long emailDeliveryId,
        long emailDeliveryAttemptId,
        string messageId,
        string businessDedupeKey,
        string toEmail,
        string templateKey,
        string provider,
        int attemptCount,
        DateTime occurredAtUtc)
    {
        if (emailDeliveryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(emailDeliveryId));
        }

        if (emailDeliveryAttemptId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(emailDeliveryAttemptId));
        }

        if (attemptCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptCount));
        }

        ValidateRequired(messageId, nameof(messageId));
        ValidateRequired(businessDedupeKey, nameof(businessDedupeKey));
        ValidateRequired(toEmail, nameof(toEmail));
        ValidateRequired(templateKey, nameof(templateKey));
        ValidateRequired(provider, nameof(provider));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidateRequired(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }
    }

    private static void ValidateRequiredDate(
        DateTime value,
        string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}