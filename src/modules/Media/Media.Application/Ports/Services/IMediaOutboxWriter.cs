using Media.Application.Outbox.Payloads;
using Media.Application.Ports.Persistence;

namespace Media.Application.Ports.Services;

public interface IMediaOutboxWriter
{
    Task<long> EnqueueMediaAssetRegisteredAsync(
        IMediaUnitOfWork unitOfWork,
        long mediaId,
        string mediaPublicId,
        string storageProvider,
        string url,
        string? storagePath,
        string? fileName,
        string mediaType,
        string? mimeType,
        long? fileSizeBytes,
        int? width,
        int? height,
        int? durationSeconds,
        string? altText,
        long actorUserId,
        long version,
        DateTime registeredAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueMediaAssetUpdatedAsync(
        IMediaUnitOfWork unitOfWork,
        long mediaId,
        string mediaPublicId,
        string? altText,
        string? metadataJson,
        long actorUserId,
        long version,
        DateTime updatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueMediaAssetSoftDeletedAsync(
        IMediaUnitOfWork unitOfWork,
        long mediaId,
        string mediaPublicId,
        bool isDeleted,
        DateTime? restoreUntil,
        int primaryClearedCount,
        long actorUserId,
        long version,
        DateTime deletedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueMediaAssetRestoredAsync(
        IMediaUnitOfWork unitOfWork,
        long mediaId,
        string mediaPublicId,
        bool isDeleted,
        long actorUserId,
        long version,
        DateTime restoredAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticleMediaAttachedAsync(
        IMediaUnitOfWork unitOfWork,
        long articleId,
        long mediaId,
        string mediaPublicId,
        long? articleMediaId,
        string url,
        string mediaType,
        string? altText,
        string? altTextOverride,
        string? caption,
        int sortOrder,
        bool isPrimary,
        bool primaryChanged,
        long actorUserId,
        long attachmentSetVersion,
        DateTime attachedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticleMediaDetachedAsync(
        IMediaUnitOfWork unitOfWork,
        long articleId,
        long mediaId,
        bool primaryCleared,
        long actorUserId,
        long attachmentSetVersion,
        DateTime detachedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticleMediaReorderedAsync(
        IMediaUnitOfWork unitOfWork,
        long articleId,
        IReadOnlyCollection<ArticleMediaReorderedItem> items,
        long actorUserId,
        long attachmentSetVersion,
        DateTime reorderedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueArticlePrimaryMediaSetAsync(
        IMediaUnitOfWork unitOfWork,
        long articleId,
        long mediaId,
        string mediaPublicId,
        string url,
        string mediaType,
        string? altText,
        string? altTextOverride,
        string? caption,
        int sortOrder,
        long actorUserId,
        long attachmentSetVersion,
        DateTime primarySetAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);
}
