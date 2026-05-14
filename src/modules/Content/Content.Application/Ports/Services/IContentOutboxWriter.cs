using Content.Application.Ports.Persistence;

namespace Content.Application.Ports.Services;

public interface IContentOutboxWriter
{
    Task<long> EnqueueArticleCreatedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        long categoryId,
        long authorUserId,
        long createdByUserId,
        string status,
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
        long actorUserId,
        long revisionId,
        string? changeSummary,
        IReadOnlyCollection<long> tagIds,
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