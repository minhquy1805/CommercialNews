using Interaction.Application.Outbox.Payloads;

namespace Interaction.Application.Ports.Services;

public interface IInteractionOutboxWriter
{
    Task WriteArticleLikedAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        ArticleLikedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteArticleUnlikedAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        ArticleUnlikedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteCommentCreatedAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        CommentCreatedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteCommentHiddenAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        CommentHiddenPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteCommentRestoredAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        CommentRestoredPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteCommentDeletedByAuthorAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        CommentDeletedByAuthorPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteCommentReportedAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        CommentReportedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteCommentReportsDismissedAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        CommentReportsDismissedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteCommentReportAlertTriggeredAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        CommentReportAlertTriggeredPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task WriteArticleCountersProjectionPublishedAsync(
        string messageId,
        string aggregatePublicId,
        long aggregateVersion,
        ArticleCountersProjectionPublishedPayload payload,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);
}