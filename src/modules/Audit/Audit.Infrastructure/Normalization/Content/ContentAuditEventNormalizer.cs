using Audit.Application.Abstractions.Serialization;
using Audit.Application.Services.Normalization;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Events;
using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Evidence;
using Audit.Infrastructure.Normalization.Common;
using Audit.Infrastructure.Normalization.Content.EventPayloads;

namespace Audit.Infrastructure.Normalization.Content;

internal sealed class ContentAuditEventNormalizer : AuditNormalizerBase
{
    public override string SourceModule => AuditSourceModules.Content;

    public ContentAuditEventNormalizer(
        IAuditJsonSerializer jsonSerializer,
        IAuditActionClassificationPolicy actionClassificationPolicy,
        IAuditRiskClassificationPolicy riskClassificationPolicy)
        : base(
            jsonSerializer,
            actionClassificationPolicy,
            riskClassificationPolicy)
    {
    }

    public override bool CanHandle(
        string eventType)
    {
        return AuditEventTypes.IsContentEvent(eventType);
    }

    protected override AuditNormalizedEvent NormalizeCore(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var eventType = NormalizeRequired(
            context.SourceEvent.EventType,
            nameof(context.SourceEvent.EventType));

        if (IsEventType(eventType, AuditEventTypes.ContentArticleCreated))
        {
            return NormalizeArticleCreated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.ContentArticleUpdated))
        {
            return NormalizeArticleUpdated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.ContentArticlePublished))
        {
            return NormalizeArticlePublished(context);
        }

        if (IsEventType(eventType, AuditEventTypes.ContentArticleUnpublished))
        {
            return NormalizeArticleUnpublished(context);
        }

        if (IsEventType(eventType, AuditEventTypes.ContentArticleArchived))
        {
            return NormalizeArticleArchived(context);
        }

        if (IsEventType(eventType, AuditEventTypes.ContentArticleSoftDeleted))
        {
            return NormalizeArticleSoftDeleted(context);
        }

        throw new InvalidOperationException(
            $"Unsupported content audit event type '{context.SourceEvent.EventType}'.");
    }

    private AuditNormalizedEvent NormalizeArticleCreated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleCreatedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.CreatedByUserId),
            resource: BuildArticleResource(payload.ArticlePublicId, payload.Title, payload.Slug),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                categoryInternalId = payload.CategoryId,
                categoryName = NormalizeOptional(payload.CategoryName),
                authorInternalId = payload.AuthorUserId,
                createdByInternalId = payload.CreatedByUserId,
                status = NormalizeRequired(payload.Status, nameof(payload.Status)),
                slug = NormalizeOptional(payload.Slug),
                canonicalUrl = NormalizeOptional(payload.CanonicalUrl),
                title = NormalizeOptional(payload.Title),
                coverMediaInternalId = payload.CoverMediaId,
                coverImageUrl = NormalizeOptional(payload.CoverImageUrl),
                tagInternalIds = payload.TagIds,
                version = payload.Version,
                createdAtUtc = payload.CreatedAtUtc
            },
            summary: "Article was created.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeArticleUpdated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleUpdatedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleResource(payload.ArticlePublicId, payload.Title, payload.Slug),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                status = NormalizeRequired(payload.Status, nameof(payload.Status)),
                categoryInternalId = payload.CategoryId,
                categoryName = NormalizeOptional(payload.CategoryName),
                authorInternalId = payload.AuthorUserId,
                actorInternalId = payload.ActorUserId,
                revisionInternalId = payload.RevisionId,
                changeSummary = NormalizeOptional(payload.ChangeSummary),
                slug = NormalizeOptional(payload.Slug),
                canonicalUrl = NormalizeOptional(payload.CanonicalUrl),
                title = NormalizeOptional(payload.Title),
                coverMediaInternalId = payload.CoverMediaId,
                coverImageUrl = NormalizeOptional(payload.CoverImageUrl),
                tagInternalIds = payload.TagIds,
                version = payload.Version,
                updatedAtUtc = payload.UpdatedAtUtc
            },
            summary: "Article was updated.",
            reason: payload.ChangeSummary);
    }

    private AuditNormalizedEvent NormalizeArticlePublished(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticlePublishedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleResource(payload.ArticlePublicId, payload.Title, payload.Slug),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                fromStatus = NormalizeRequired(payload.FromStatus, nameof(payload.FromStatus)),
                toStatus = NormalizeRequired(payload.ToStatus, nameof(payload.ToStatus)),
                categoryInternalId = payload.CategoryId,
                categoryName = NormalizeOptional(payload.CategoryName),
                authorInternalId = payload.AuthorUserId,
                actorInternalId = payload.ActorUserId,
                slug = NormalizeOptional(payload.Slug),
                canonicalUrl = NormalizeOptional(payload.CanonicalUrl),
                title = NormalizeOptional(payload.Title),
                coverMediaInternalId = payload.CoverMediaId,
                coverImageUrl = NormalizeOptional(payload.CoverImageUrl),
                tagInternalIds = payload.TagIds,
                version = payload.Version,
                publishedAtUtc = payload.PublishedAtUtc
            },
            summary: "Article was published.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeArticleUnpublished(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleUnpublishedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleResource(payload.ArticlePublicId, title: null, slug: null),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                fromStatus = NormalizeRequired(payload.FromStatus, nameof(payload.FromStatus)),
                toStatus = NormalizeRequired(payload.ToStatus, nameof(payload.ToStatus)),
                reason = NormalizeRequired(payload.Reason, nameof(payload.Reason)),
                actorInternalId = payload.ActorUserId,
                version = payload.Version,
                unpublishedAtUtc = payload.UnpublishedAtUtc
            },
            summary: "Article was unpublished.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent NormalizeArticleArchived(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleArchivedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleResource(payload.ArticlePublicId, title: null, slug: null),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                fromStatus = NormalizeRequired(payload.FromStatus, nameof(payload.FromStatus)),
                toStatus = NormalizeRequired(payload.ToStatus, nameof(payload.ToStatus)),
                actorInternalId = payload.ActorUserId,
                version = payload.Version,
                archivedAtUtc = payload.ArchivedAtUtc
            },
            summary: "Article was archived.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeArticleSoftDeleted(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleSoftDeletedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleResource(payload.ArticlePublicId, title: null, slug: null),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                fromStatus = NormalizeRequired(payload.FromStatus, nameof(payload.FromStatus)),
                toStatus = NormalizeRequired(payload.ToStatus, nameof(payload.ToStatus)),
                isDeleted = payload.IsDeleted,
                actorInternalId = payload.ActorUserId,
                version = payload.Version,
                deletedAtUtc = payload.DeletedAtUtc
            },
            summary: "Article was soft deleted.",
            reason: null);
    }

    private AuditNormalizedEvent CreateNormalizedEvent<TPayload>(
        AuditEventNormalizationContext context,
        AuditActor actor,
        AuditResource resource,
        TPayload normalizedPayload,
        string summary,
        string? reason)
    {
        var actionClassification = ClassifyAction(context.SourceEvent.EventType);
        var riskClassification = ClassifyRisk(context.SourceEvent.EventType, actionClassification);

        return new AuditNormalizedEvent(
            Actor: actor,
            Resource: resource,
            ActionClassification: actionClassification,
            RiskClassification: riskClassification,
            RequestContext: AuditRequestContext.Empty(),
            JsonPayload: AuditJsonPayload.Create(
                metadataJson: null,
                headersJson: context.HeadersJson,
                sanitizedPayloadJson: Serialize(normalizedPayload),
                beforeJson: null,
                afterJson: null,
                changesJson: null),
            Summary: summary,
            Reason: NormalizeOptional(reason));
    }

    private static AuditActor BuildUserActor(
        long actorInternalId)
    {
        return AuditActor.Create(
            actorInternalId: actorInternalId,
            actorUserId: null,
            actorEmail: null,
            actorDisplayName: null,
            actorType: AuditActorTypes.User);
    }

    private static AuditResource BuildArticleResource(
        string articlePublicId,
        string? title,
        string? slug)
    {
        var normalizedArticlePublicId = NormalizeRequired(
            articlePublicId,
            nameof(articlePublicId));

        return AuditResource.Create(
            AuditResourceTypes.Article,
            normalizedArticlePublicId,
            ResolveDisplayName(title, slug, normalizedArticlePublicId));
    }

    private static string ResolveDisplayName(
        string? primary,
        string? secondary,
        string fallback)
    {
        return NormalizeOptional(primary) ??
            NormalizeOptional(secondary) ??
            NormalizeRequired(fallback, nameof(fallback));
    }

    private static bool IsEventType(
        string eventType,
        string expectedEventType)
    {
        return string.Equals(eventType, expectedEventType, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRequired(
        string? value,
        string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                $"Content audit payload field '{parameterName}' is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
