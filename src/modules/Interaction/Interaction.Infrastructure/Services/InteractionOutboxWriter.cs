using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using Interaction.Application.Outbox;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;

namespace Interaction.Infrastructure.Services;

public sealed class InteractionOutboxWriter : IInteractionOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    public InteractionOutboxWriter(
        IInteractionUnitOfWork unitOfWork,
        IOutboxMessageRepository outboxMessageRepository)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
    }

    public Task WriteArticleLikedAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        ArticleLikedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.ArticleLiked,
            aggregateType: InteractionAggregateTypes.ArticleLike,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteArticleUnlikedAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        ArticleUnlikedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.ArticleUnliked,
            aggregateType: InteractionAggregateTypes.ArticleLike,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteCommentCreatedAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        CommentCreatedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.CommentCreated,
            aggregateType: InteractionAggregateTypes.Comment,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteCommentHiddenAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        CommentHiddenPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.CommentHidden,
            aggregateType: InteractionAggregateTypes.Comment,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteCommentRestoredAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        CommentRestoredPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.CommentRestored,
            aggregateType: InteractionAggregateTypes.Comment,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteCommentDeletedByAuthorAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        CommentDeletedByAuthorPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.CommentDeletedByAuthor,
            aggregateType: InteractionAggregateTypes.Comment,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteCommentReportedAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        CommentReportedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.CommentReported,
            aggregateType: InteractionAggregateTypes.CommentReport,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteCommentReportsDismissedAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        CommentReportsDismissedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.CommentReportsDismissed,
            aggregateType: InteractionAggregateTypes.CommentModerationCase,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteCommentReportAlertTriggeredAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        CommentReportAlertTriggeredPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.CommentReportAlertTriggered,
            aggregateType: InteractionAggregateTypes.CommentModerationCase,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    public Task WriteArticleCountersProjectionPublishedAsync(
        string messageId,
        string aggregatePublicId,
        int aggregateVersion,
        ArticleCountersProjectionPublishedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return InsertOutboxMessageAsync(
            messageId: messageId,
            eventType: InteractionIntegrationEventTypes.ArticleCountersProjectionPublished,
            aggregateType: InteractionAggregateTypes.ArticleInteractionStats,
            aggregatePublicId: aggregatePublicId,
            aggregateVersion: aggregateVersion,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: initiatorUserId,
            cancellationToken: cancellationToken);
    }

    private async Task InsertOutboxMessageAsync<TPayload>(
        string messageId,
        string eventType,
        string aggregateType,
        string aggregatePublicId,
        int aggregateVersion,
        TPayload payload,
        DateTime occurredAtUtc,
        byte priority,
        string? correlationId,
        long? initiatorUserId,
        CancellationToken cancellationToken)
    {
        if (!_unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Interaction outbox message must be written inside an active transaction.");
        }

        ValidatePublicId(messageId, nameof(messageId));
        ValidateRequired(eventType, nameof(eventType));
        ValidateRequired(aggregateType, nameof(aggregateType));
        ValidatePublicId(aggregatePublicId, nameof(aggregatePublicId));
        ValidatePositiveVersion(aggregateVersion, nameof(aggregateVersion));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));

        string normalizedMessageId = messageId.Trim();
        string normalizedAggregatePublicId = aggregatePublicId.Trim();

        string payloadJson =
            JsonSerializer.Serialize(payload, JsonOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: normalizedMessageId,
            eventType: eventType.Trim(),
            aggregateType: aggregateType.Trim(),
            aggregateId: normalizedAggregatePublicId,
            payload: payloadJson,
            occurredAt: occurredAtUtc,
            priority: priority,
            aggregatePublicId: normalizedAggregatePublicId,
            aggregateVersion: aggregateVersion,
            headers: null,
            correlationId: NormalizeOptional(correlationId),
            initiatorUserId: initiatorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static void ValidatePublicId(
        string? value,
        string parameterName)
    {
        ValidateRequired(value, parameterName);

        if (value!.Trim().Length != 26)
        {
            throw new ArgumentException(
                $"{parameterName} must be exactly 26 characters.",
                parameterName);
        }
    }

    private static void ValidateRequired(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }
    }

    private static void ValidatePositiveVersion(
        int value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Aggregate version must be greater than zero.");
        }
    }

    private static void ValidateRequiredDate(
        DateTime value,
        string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
