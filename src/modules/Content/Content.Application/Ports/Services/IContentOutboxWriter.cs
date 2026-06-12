using Content.Application.Ports.Persistence;
using Content.Application.Outbox.Payloads;

namespace Content.Application.Ports.Services;

public interface IContentOutboxWriter
{
    Task<long> EnqueueArticleCreatedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        long categoryId,
        string? categoryName,
        long authorUserId,
        long createdByUserId,
        string status,
        string? slug,
        string? canonicalUrl,
        string? title,
        string? summary,
        string? body,
        long? coverMediaId,
        string? coverImageUrl,
        IReadOnlyCollection<long> tagIds,
        long version,
        DateTime createdAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticleUpdatedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        string status,
        long categoryId,
        string? categoryName,
        long authorUserId,
        long actorUserId,
        long revisionId,
        string? changeSummary,
        string? slug,
        string? canonicalUrl,
        string? title,
        string? summary,
        string? body,
        long? coverMediaId,
        string? coverImageUrl,
        IReadOnlyCollection<long> tagIds,
        IReadOnlyCollection<ArticleTagIntegrationEventPayload> tags,
        long version,
        DateTime updatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticlePublishedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        string fromStatus,
        string toStatus,
        long categoryId,
        string? categoryName,
        long authorUserId,
        string? slug,
        string? canonicalUrl,
        string? title,
        string? summary,
        string? body,
        long? coverMediaId,
        string? coverImageUrl,
        IReadOnlyCollection<long> tagIds,
        IReadOnlyCollection<ArticleTagIntegrationEventPayload> tags,
        long actorUserId,
        long version,
        DateTime publishedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticleUnpublishedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        string fromStatus,
        string toStatus,
        string reason,
        long actorUserId,
        long version,
        DateTime unpublishedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticleArchivedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        string fromStatus,
        string toStatus,
        long actorUserId,
        long version,
        DateTime archivedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticleSoftDeletedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        string fromStatus,
        string toStatus,
        bool isDeleted,
        long actorUserId,
        long version,
        DateTime deletedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);
}
