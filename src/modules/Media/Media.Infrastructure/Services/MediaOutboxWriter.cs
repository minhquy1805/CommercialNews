using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using Media.Application.Outbox;
using Media.Application.Outbox.Payloads;
using Media.Application.Ports.Persistence;
using Media.Application.Ports.Services;

namespace Media.Infrastructure.Services;

public sealed class MediaOutboxWriter : IMediaOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public MediaOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public Task<long> EnqueueMediaAssetRegisteredAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateMediaAssetEnvelope(
            mediaId,
            mediaPublicId,
            actorUserId,
            version,
            registeredAtUtc);

        ValidateRequired(storageProvider, nameof(storageProvider));
        ValidateRequired(url, nameof(url));
        ValidateRequired(mediaType, nameof(mediaType));

        string normalizedMediaPublicId = mediaPublicId.Trim();

        string businessDedupeKey = BuildMediaAssetBusinessDedupeKey(
            normalizedMediaPublicId,
            "registered",
            version);

        var payload = new MediaAssetRegisteredIntegrationEventPayload(
            MediaId: mediaId,
            MediaPublicId: normalizedMediaPublicId,
            StorageProvider: storageProvider.Trim(),
            Url: url.Trim(),
            StoragePath: NormalizeOptional(storagePath),
            FileName: NormalizeOptional(fileName),
            MediaType: mediaType.Trim(),
            MimeType: NormalizeOptional(mimeType),
            FileSizeBytes: fileSizeBytes,
            Width: width,
            Height: height,
            DurationSeconds: durationSeconds,
            AltText: NormalizeOptional(altText),
            ActorUserId: actorUserId,
            Version: version,
            RegisteredAtUtc: registeredAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.AssetRegistered,
            aggregateType: MediaAggregateTypes.MediaAsset,
            aggregateId: normalizedMediaPublicId,
            aggregatePublicId: normalizedMediaPublicId,
            aggregateVersion: version,
            payload: payload,
            occurredAtUtc: registeredAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueMediaAssetUpdatedAsync(
        IMediaUnitOfWork unitOfWork,
        long mediaId,
        string mediaPublicId,
        string? altText,
        string? metadataJson,
        long actorUserId,
        long version,
        DateTime updatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateMediaAssetEnvelope(
            mediaId,
            mediaPublicId,
            actorUserId,
            version,
            updatedAtUtc);

        string normalizedMediaPublicId = mediaPublicId.Trim();

        string businessDedupeKey = BuildMediaAssetBusinessDedupeKey(
            normalizedMediaPublicId,
            "updated",
            version);

        var payload = new MediaAssetUpdatedIntegrationEventPayload(
            MediaId: mediaId,
            MediaPublicId: normalizedMediaPublicId,
            AltText: NormalizeOptional(altText),
            MetadataJson: NormalizeOptional(metadataJson),
            ActorUserId: actorUserId,
            Version: version,
            UpdatedAtUtc: updatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.AssetUpdated,
            aggregateType: MediaAggregateTypes.MediaAsset,
            aggregateId: normalizedMediaPublicId,
            aggregatePublicId: normalizedMediaPublicId,
            aggregateVersion: version,
            payload: payload,
            occurredAtUtc: updatedAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueMediaAssetSoftDeletedAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateMediaAssetEnvelope(
            mediaId,
            mediaPublicId,
            actorUserId,
            version,
            deletedAtUtc);

        if (primaryClearedCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(primaryClearedCount));
        }

        string normalizedMediaPublicId = mediaPublicId.Trim();

        string businessDedupeKey = BuildMediaAssetBusinessDedupeKey(
            normalizedMediaPublicId,
            "soft_deleted",
            version);

        var payload = new MediaAssetSoftDeletedIntegrationEventPayload(
            MediaId: mediaId,
            MediaPublicId: normalizedMediaPublicId,
            IsDeleted: isDeleted,
            RestoreUntil: restoreUntil,
            PrimaryClearedCount: primaryClearedCount,
            ActorUserId: actorUserId,
            Version: version,
            DeletedAtUtc: deletedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.AssetSoftDeleted,
            aggregateType: MediaAggregateTypes.MediaAsset,
            aggregateId: normalizedMediaPublicId,
            aggregatePublicId: normalizedMediaPublicId,
            aggregateVersion: version,
            payload: payload,
            occurredAtUtc: deletedAtUtc,
            priority: 1,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueMediaAssetRestoredAsync(
        IMediaUnitOfWork unitOfWork,
        long mediaId,
        string mediaPublicId,
        bool isDeleted,
        long actorUserId,
        long version,
        DateTime restoredAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateMediaAssetEnvelope(
            mediaId,
            mediaPublicId,
            actorUserId,
            version,
            restoredAtUtc);

        string normalizedMediaPublicId = mediaPublicId.Trim();

        string businessDedupeKey = BuildMediaAssetBusinessDedupeKey(
            normalizedMediaPublicId,
            "restored",
            version);

        var payload = new MediaAssetRestoredIntegrationEventPayload(
            MediaId: mediaId,
            MediaPublicId: normalizedMediaPublicId,
            IsDeleted: isDeleted,
            ActorUserId: actorUserId,
            Version: version,
            RestoredAtUtc: restoredAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.AssetRestored,
            aggregateType: MediaAggregateTypes.MediaAsset,
            aggregateId: normalizedMediaPublicId,
            aggregatePublicId: normalizedMediaPublicId,
            aggregateVersion: version,
            payload: payload,
            occurredAtUtc: restoredAtUtc,
            priority: 2,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticleMediaAttachedAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateArticleMediaSetEnvelope(
            articleId,
            mediaId,
            actorUserId,
            attachmentSetVersion,
            attachedAtUtc);

        ValidateRequired(mediaPublicId, nameof(mediaPublicId));
        ValidateRequired(url, nameof(url));
        ValidateRequired(mediaType, nameof(mediaType));
        ValidateNonNegative(sortOrder, nameof(sortOrder));

        string normalizedMediaPublicId = mediaPublicId.Trim();
        string? normalizedAltText = NormalizeOptional(altText);
        string? normalizedAltTextOverride = NormalizeOptional(altTextOverride);

        string businessDedupeKey = BuildArticleMediaSetBusinessDedupeKey(
            articleId,
            mediaId,
            "attached",
            attachmentSetVersion);

        var payload = new ArticleMediaAttachedIntegrationEventPayload(
            ArticleId: articleId,
            MediaId: mediaId,
            MediaPublicId: normalizedMediaPublicId,
            ArticleMediaId: articleMediaId,
            Url: url.Trim(),
            MediaType: mediaType.Trim(),
            AltText: normalizedAltText,
            AltTextOverride: normalizedAltTextOverride,
            EffectiveAltText: normalizedAltTextOverride ?? normalizedAltText,
            Caption: NormalizeOptional(caption),
            SortOrder: sortOrder,
            IsPrimary: isPrimary,
            PrimaryChanged: primaryChanged,
            ActorUserId: actorUserId,
            AttachmentSetVersion: attachmentSetVersion,
            AttachedAtUtc: attachedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.ArticleMediaAttached,
            aggregateType: MediaAggregateTypes.ArticleMediaSet,
            aggregateId: articleId.ToString(),
            aggregatePublicId: null,
            aggregateVersion: attachmentSetVersion,
            payload: payload,
            occurredAtUtc: attachedAtUtc,
            priority: 2,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticleMediaDetachedAsync(
        IMediaUnitOfWork unitOfWork,
        long articleId,
        long mediaId,
        bool primaryCleared,
        long actorUserId,
        long attachmentSetVersion,
        DateTime detachedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateArticleMediaSetEnvelope(
            articleId,
            mediaId,
            actorUserId,
            attachmentSetVersion,
            detachedAtUtc);

        string businessDedupeKey = BuildArticleMediaSetBusinessDedupeKey(
            articleId,
            mediaId,
            "detached",
            attachmentSetVersion);

        var payload = new ArticleMediaDetachedIntegrationEventPayload(
            ArticleId: articleId,
            MediaId: mediaId,
            PrimaryCleared: primaryCleared,
            ActorUserId: actorUserId,
            AttachmentSetVersion: attachmentSetVersion,
            DetachedAtUtc: detachedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.ArticleMediaDetached,
            aggregateType: MediaAggregateTypes.ArticleMediaSet,
            aggregateId: articleId.ToString(),
            aggregatePublicId: null,
            aggregateVersion: attachmentSetVersion,
            payload: payload,
            occurredAtUtc: detachedAtUtc,
            priority: 2,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticleMediaReorderedAsync(
        IMediaUnitOfWork unitOfWork,
        long articleId,
        IReadOnlyCollection<ArticleMediaReorderedItem> items,
        long actorUserId,
        long attachmentSetVersion,
        DateTime reorderedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(items);

        ValidatePositiveId(articleId, nameof(articleId));
        ValidatePositiveId(actorUserId, nameof(actorUserId));
        ValidatePositiveId(attachmentSetVersion, nameof(attachmentSetVersion));
        ValidateRequiredDate(reorderedAtUtc, nameof(reorderedAtUtc));

        if (items.Count == 0)
        {
            throw new ArgumentException(
                "Reordered items are required.",
                nameof(items));
        }

        string businessDedupeKey = BuildArticleMediaSetBusinessDedupeKey(
            articleId,
            mediaId: null,
            action: "reordered",
            version: attachmentSetVersion);

        var payload = new ArticleMediaReorderedIntegrationEventPayload(
            ArticleId: articleId,
            Items: items.ToArray(),
            ActorUserId: actorUserId,
            AttachmentSetVersion: attachmentSetVersion,
            ReorderedAtUtc: reorderedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.ArticleMediaReordered,
            aggregateType: MediaAggregateTypes.ArticleMediaSet,
            aggregateId: articleId.ToString(),
            aggregatePublicId: null,
            aggregateVersion: attachmentSetVersion,
            payload: payload,
            occurredAtUtc: reorderedAtUtc,
            priority: 2,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticlePrimaryMediaSetAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateArticleMediaSetEnvelope(
            articleId,
            mediaId,
            actorUserId,
            attachmentSetVersion,
            primarySetAtUtc);

        ValidateRequired(mediaPublicId, nameof(mediaPublicId));
        ValidateRequired(url, nameof(url));
        ValidateRequired(mediaType, nameof(mediaType));
        ValidateNonNegative(sortOrder, nameof(sortOrder));

        string normalizedMediaPublicId = mediaPublicId.Trim();
        string? normalizedAltText = NormalizeOptional(altText);
        string? normalizedAltTextOverride = NormalizeOptional(altTextOverride);

        string businessDedupeKey = BuildArticleMediaSetBusinessDedupeKey(
            articleId,
            mediaId,
            "primary_set",
            attachmentSetVersion);

        var payload = new ArticlePrimaryMediaSetIntegrationEventPayload(
            ArticleId: articleId,
            MediaId: mediaId,
            MediaPublicId: normalizedMediaPublicId,
            Url: url.Trim(),
            MediaType: mediaType.Trim(),
            AltText: normalizedAltText,
            AltTextOverride: normalizedAltTextOverride,
            EffectiveAltText: normalizedAltTextOverride ?? normalizedAltText,
            Caption: NormalizeOptional(caption),
            SortOrder: sortOrder,
            ActorUserId: actorUserId,
            AttachmentSetVersion: attachmentSetVersion,
            PrimarySetAtUtc: primarySetAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: MediaIntegrationEventTypes.ArticlePrimaryMediaSet,
            aggregateType: MediaAggregateTypes.ArticleMediaSet,
            aggregateId: articleId.ToString(),
            aggregatePublicId: null,
            aggregateVersion: attachmentSetVersion,
            payload: payload,
            occurredAtUtc: primarySetAtUtc,
            priority: 2,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    private async Task<long> InsertOutboxMessageAsync<TPayload>(
        IMediaUnitOfWork unitOfWork,
        string eventType,
        string aggregateType,
        string aggregateId,
        string? aggregatePublicId,
        long aggregateVersion,
        TPayload payload,
        DateTime occurredAtUtc,
        byte priority,
        string? correlationId,
        long? initiatorUserId,
        CancellationToken cancellationToken)
    {
        if (!unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Media outbox message must be written inside an active transaction.");
        }

        ValidateRequired(eventType, nameof(eventType));
        ValidateRequired(aggregateType, nameof(aggregateType));
        ValidateRequired(aggregateId, nameof(aggregateId));
        ValidatePositiveId(aggregateVersion, nameof(aggregateVersion));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));

        string payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: eventType,
            aggregateType: aggregateType.Trim(),
            aggregateId: aggregateId.Trim(),
            payload: payloadJson,
            occurredAt: occurredAtUtc,
            priority: priority,
            aggregatePublicId: NormalizeOptional(aggregatePublicId),
            aggregateVersion: ToAggregateVersion(aggregateVersion),
            headers: null,
            correlationId: NormalizeOptional(correlationId),
            initiatorUserId: initiatorUserId);

        return await _outboxMessageRepository.InsertAsync(
            unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static void ValidateMediaAssetEnvelope(
        long mediaId,
        string mediaPublicId,
        long actorUserId,
        long version,
        DateTime occurredAtUtc)
    {
        ValidatePositiveId(mediaId, nameof(mediaId));
        ValidateRequired(mediaPublicId, nameof(mediaPublicId));
        ValidatePositiveId(actorUserId, nameof(actorUserId));
        ValidatePositiveId(version, nameof(version));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidateArticleMediaSetEnvelope(
        long articleId,
        long mediaId,
        long actorUserId,
        long attachmentSetVersion,
        DateTime occurredAtUtc)
    {
        ValidatePositiveId(articleId, nameof(articleId));
        ValidatePositiveId(mediaId, nameof(mediaId));
        ValidatePositiveId(actorUserId, nameof(actorUserId));
        ValidatePositiveId(attachmentSetVersion, nameof(attachmentSetVersion));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static string BuildMediaAssetBusinessDedupeKey(
        string mediaPublicId,
        string action,
        long version)
    {
        return $"media:asset:{mediaPublicId.Trim()}:{action}:v{version}";
    }

    private static string BuildArticleMediaSetBusinessDedupeKey(
        long articleId,
        long? mediaId,
        string action,
        long version)
    {
        return mediaId.HasValue
            ? $"media:article-media-set:{articleId}:media:{mediaId.Value}:{action}:v{version}"
            : $"media:article-media-set:{articleId}:{action}:v{version}";
    }

    private static int ToAggregateVersion(long version)
    {
        if (version > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version),
                "Aggregate version exceeds Int32 range.");
        }

        return (int)version;
    }

    private static void ValidatePositiveId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }
    }

    private static void ValidateNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateRequiredDate(DateTime value, string parameterName)
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
