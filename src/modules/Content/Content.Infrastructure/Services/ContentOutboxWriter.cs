using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using Content.Application.Outbox;
using Content.Application.Outbox.Payloads;
using Content.Application.Ports.Persistence;
using Content.Application.Ports.Services;

namespace Content.Infrastructure.Services;

public sealed class ContentOutboxWriter : IContentOutboxWriter
{
    private const string AggregateTypeArticle = "Content.Article";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public ContentOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public Task<long> EnqueueArticleCreatedAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(tagIds);

        ValidateArticleEnvelope(articleId, articlePublicId, version, createdAtUtc);
        ValidatePositiveId(categoryId, nameof(categoryId));
        ValidatePositiveId(authorUserId, nameof(authorUserId));
        ValidatePositiveId(createdByUserId, nameof(createdByUserId));
        ValidateRequired(status, nameof(status));
        ValidateOptionalPositiveId(coverMediaId, nameof(coverMediaId));

        string normalizedArticlePublicId = articlePublicId.Trim();
        string? normalizedSlug = NormalizeOptional(slug);
        string? normalizedCanonicalUrl = NormalizeOptional(canonicalUrl);
        string? normalizedTitle = NormalizeOptional(title);
        string? normalizedSummary = NormalizeOptional(summary);
        string? normalizedBody = NormalizeOptional(body);
        string? normalizedCoverImageUrl = NormalizeOptional(coverImageUrl);
        string? normalizedCategoryName = NormalizeOptional(categoryName);

        string businessDedupeKey = BuildArticleBusinessDedupeKey(
            normalizedArticlePublicId,
            "created",
            version);

        var payload = new ArticleCreatedIntegrationEventPayload(
            ArticleId: articleId,
            ArticlePublicId: normalizedArticlePublicId,
            CategoryId: categoryId,
            CategoryName: normalizedCategoryName,
            AuthorUserId: authorUserId,
            CreatedByUserId: createdByUserId,
            Status: status.Trim(),
            Slug: normalizedSlug,
            CanonicalUrl: normalizedCanonicalUrl,
            Title: normalizedTitle,
            Summary: normalizedSummary,
            Body: normalizedBody,
            CoverMediaId: coverMediaId,
            CoverImageUrl: normalizedCoverImageUrl,
            TagIds: tagIds.ToArray(),
            Version: version,
            CreatedAtUtc: createdAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: ContentIntegrationEventTypes.ArticleCreated,
            aggregateType: AggregateTypeArticle,
            aggregateId: normalizedArticlePublicId,
            aggregatePublicId: normalizedArticlePublicId,
            aggregateVersion: ToAggregateVersion(version),
            payload: payload,
            occurredAtUtc: createdAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: createdByUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticleUpdatedAsync(
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
        long version,
        DateTime updatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(tagIds);

        ValidateArticleEnvelope(articleId, articlePublicId, version, updatedAtUtc);
        ValidateRequired(status, nameof(status));
        ValidatePositiveId(categoryId, nameof(categoryId));
        ValidatePositiveId(authorUserId, nameof(authorUserId));
        ValidatePositiveId(actorUserId, nameof(actorUserId));
        ValidatePositiveId(revisionId, nameof(revisionId));
        ValidateOptionalPositiveId(coverMediaId, nameof(coverMediaId));

        string normalizedArticlePublicId = articlePublicId.Trim();
        string? normalizedSlug = NormalizeOptional(slug);
        string? normalizedCanonicalUrl = NormalizeOptional(canonicalUrl);
        string? normalizedTitle = NormalizeOptional(title);
        string? normalizedSummary = NormalizeOptional(summary);
        string? normalizedBody = NormalizeOptional(body);
        string? normalizedCoverImageUrl = NormalizeOptional(coverImageUrl);
        string? normalizedCategoryName = NormalizeOptional(categoryName);

        string businessDedupeKey = BuildArticleBusinessDedupeKey(
            normalizedArticlePublicId,
            "updated",
            version);

        var payload = new ArticleUpdatedIntegrationEventPayload(
            ArticleId: articleId,
            ArticlePublicId: normalizedArticlePublicId,
            Status: status.Trim(),
            CategoryId: categoryId,
            CategoryName: normalizedCategoryName,
            AuthorUserId: authorUserId,
            ActorUserId: actorUserId,
            RevisionId: revisionId,
            ChangeSummary: NormalizeOptional(changeSummary),
            Slug: normalizedSlug,
            CanonicalUrl: normalizedCanonicalUrl,
            Title: normalizedTitle,
            Summary: normalizedSummary,
            Body: normalizedBody,
            CoverMediaId: coverMediaId,
            CoverImageUrl: normalizedCoverImageUrl,
            TagIds: tagIds.ToArray(),
            Version: version,
            UpdatedAtUtc: updatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: ContentIntegrationEventTypes.ArticleUpdated,
            aggregateType: AggregateTypeArticle,
            aggregateId: normalizedArticlePublicId,
            aggregatePublicId: normalizedArticlePublicId,
            aggregateVersion: ToAggregateVersion(version),
            payload: payload,
            occurredAtUtc: updatedAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticlePublishedAsync(
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
        long actorUserId,
        long version,
        DateTime publishedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(tagIds);

        ValidateArticleLifecycleEnvelope(
            articleId,
            articlePublicId,
            fromStatus,
            toStatus,
            actorUserId,
            version,
            publishedAtUtc);

        ValidatePositiveId(categoryId, nameof(categoryId));
        ValidatePositiveId(authorUserId, nameof(authorUserId));
        ValidateOptionalPositiveId(coverMediaId, nameof(coverMediaId));

        string normalizedArticlePublicId = articlePublicId.Trim();
        string? normalizedSlug = NormalizeOptional(slug);
        string? normalizedCanonicalUrl = NormalizeOptional(canonicalUrl);
        string? normalizedTitle = NormalizeOptional(title);
        string? normalizedSummary = NormalizeOptional(summary);
        string? normalizedBody = NormalizeOptional(body);
        string? normalizedCoverImageUrl = NormalizeOptional(coverImageUrl);
        string? normalizedCategoryName = NormalizeOptional(categoryName);

        string businessDedupeKey = BuildArticleBusinessDedupeKey(
            normalizedArticlePublicId,
            "published",
            version);

        var payload = new ArticlePublishedIntegrationEventPayload(
            ArticleId: articleId,
            ArticlePublicId: normalizedArticlePublicId,
            FromStatus: fromStatus.Trim(),
            ToStatus: toStatus.Trim(),
            CategoryId: categoryId,
            CategoryName: normalizedCategoryName,
            AuthorUserId: authorUserId,
            Slug: normalizedSlug,
            CanonicalUrl: normalizedCanonicalUrl,
            Title: normalizedTitle,
            Summary: normalizedSummary,
            Body: normalizedBody,
            CoverMediaId: coverMediaId,
            CoverImageUrl: normalizedCoverImageUrl,
            TagIds: tagIds.ToArray(),
            ActorUserId: actorUserId,
            Version: version,
            PublishedAtUtc: publishedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: ContentIntegrationEventTypes.ArticlePublished,
            aggregateType: AggregateTypeArticle,
            aggregateId: normalizedArticlePublicId,
            aggregatePublicId: normalizedArticlePublicId,
            aggregateVersion: ToAggregateVersion(version),
            payload: payload,
            occurredAtUtc: publishedAtUtc,
            priority: 2,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticleUnpublishedAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateArticleLifecycleEnvelope(
            articleId,
            articlePublicId,
            fromStatus,
            toStatus,
            actorUserId,
            version,
            unpublishedAtUtc);

        ValidateRequired(reason, nameof(reason));

        string normalizedArticlePublicId = articlePublicId.Trim();

        string businessDedupeKey = BuildArticleBusinessDedupeKey(
            normalizedArticlePublicId,
            "unpublished",
            version);

        var payload = new ArticleUnpublishedIntegrationEventPayload(
            ArticleId: articleId,
            ArticlePublicId: normalizedArticlePublicId,
            FromStatus: fromStatus.Trim(),
            ToStatus: toStatus.Trim(),
            Reason: reason.Trim(),
            ActorUserId: actorUserId,
            Version: version,
            UnpublishedAtUtc: unpublishedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: ContentIntegrationEventTypes.ArticleUnpublished,
            aggregateType: AggregateTypeArticle,
            aggregateId: normalizedArticlePublicId,
            aggregatePublicId: normalizedArticlePublicId,
            aggregateVersion: ToAggregateVersion(version),
            payload: payload,
            occurredAtUtc: unpublishedAtUtc,
            priority: 1,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticleArchivedAsync(
        IContentUnitOfWork unitOfWork,
        long articleId,
        string articlePublicId,
        string fromStatus,
        string toStatus,
        long actorUserId,
        long version,
        DateTime archivedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateArticleLifecycleEnvelope(
            articleId,
            articlePublicId,
            fromStatus,
            toStatus,
            actorUserId,
            version,
            archivedAtUtc);

        string normalizedArticlePublicId = articlePublicId.Trim();

        string businessDedupeKey = BuildArticleBusinessDedupeKey(
            normalizedArticlePublicId,
            "archived",
            version);

        var payload = new ArticleArchivedIntegrationEventPayload(
            ArticleId: articleId,
            ArticlePublicId: normalizedArticlePublicId,
            FromStatus: fromStatus.Trim(),
            ToStatus: toStatus.Trim(),
            ActorUserId: actorUserId,
            Version: version,
            ArchivedAtUtc: archivedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: ContentIntegrationEventTypes.ArticleArchived,
            aggregateType: AggregateTypeArticle,
            aggregateId: normalizedArticlePublicId,
            aggregatePublicId: normalizedArticlePublicId,
            aggregateVersion: ToAggregateVersion(version),
            payload: payload,
            occurredAtUtc: archivedAtUtc,
            priority: 1,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueArticleSoftDeletedAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateArticleLifecycleEnvelope(
            articleId,
            articlePublicId,
            fromStatus,
            toStatus,
            actorUserId,
            version,
            deletedAtUtc);

        string normalizedArticlePublicId = articlePublicId.Trim();

        string businessDedupeKey = BuildArticleBusinessDedupeKey(
            normalizedArticlePublicId,
            "soft_deleted",
            version);

        var payload = new ArticleSoftDeletedIntegrationEventPayload(
            ArticleId: articleId,
            ArticlePublicId: normalizedArticlePublicId,
            FromStatus: fromStatus.Trim(),
            ToStatus: toStatus.Trim(),
            IsDeleted: isDeleted,
            ActorUserId: actorUserId,
            Version: version,
            DeletedAtUtc: deletedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: ContentIntegrationEventTypes.ArticleSoftDeleted,
            aggregateType: AggregateTypeArticle,
            aggregateId: normalizedArticlePublicId,
            aggregatePublicId: normalizedArticlePublicId,
            aggregateVersion: ToAggregateVersion(version),
            payload: payload,
            occurredAtUtc: deletedAtUtc,
            priority: 1,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    private async Task<long> InsertOutboxMessageAsync<TPayload>(
        IContentUnitOfWork unitOfWork,
        string eventType,
        string aggregateType,
        string aggregateId,
        string? aggregatePublicId,
        int aggregateVersion,
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
                "Content outbox message must be written inside an active transaction.");
        }

        string payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: eventType,
            aggregateType: aggregateType,
            aggregateId: aggregateId.Trim(),
            payload: payloadJson,
            occurredAt: occurredAtUtc,
            priority: priority,
            aggregatePublicId: NormalizeOptional(aggregatePublicId),
            aggregateVersion: aggregateVersion,
            headers: null,
            correlationId: NormalizeOptional(correlationId),
            initiatorUserId: initiatorUserId);

        return await _outboxMessageRepository.InsertAsync(
            unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static void ValidateArticleEnvelope(
        long articleId,
        string articlePublicId,
        long version,
        DateTime occurredAtUtc)
    {
        ValidatePositiveId(articleId, nameof(articleId));
        ValidateRequired(articlePublicId, nameof(articlePublicId));
        ValidatePositiveId(version, nameof(version));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidateArticleLifecycleEnvelope(
        long articleId,
        string articlePublicId,
        string fromStatus,
        string toStatus,
        long actorUserId,
        long version,
        DateTime occurredAtUtc)
    {
        ValidateArticleEnvelope(
            articleId,
            articlePublicId,
            version,
            occurredAtUtc);

        ValidateRequired(fromStatus, nameof(fromStatus));
        ValidateRequired(toStatus, nameof(toStatus));
        ValidatePositiveId(actorUserId, nameof(actorUserId));
    }

    private static string BuildArticleBusinessDedupeKey(
        string articlePublicId,
        string action,
        long version)
    {
        return $"content:article:{articlePublicId.Trim()}:{action}:v{version}";
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

    private static void ValidateOptionalPositiveId(
        long? value,
        string parameterName)
    {
        if (value.HasValue && value.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
