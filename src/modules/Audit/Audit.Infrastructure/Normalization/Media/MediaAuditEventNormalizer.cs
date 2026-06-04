using Audit.Application.Abstractions.Serialization;
using Audit.Application.Services.Normalization;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Events;
using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Evidence;
using Audit.Infrastructure.Normalization.Common;
using Audit.Infrastructure.Normalization.Media.EventPayloads;

namespace Audit.Infrastructure.Normalization.Media;

internal sealed class MediaAuditEventNormalizer : AuditNormalizerBase
{
    public override string SourceModule => AuditSourceModules.Media;

    public MediaAuditEventNormalizer(
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
        return AuditEventTypes.IsMediaEvent(eventType);
    }

    protected override AuditNormalizedEvent NormalizeCore(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var eventType = NormalizeRequired(
            context.SourceEvent.EventType,
            nameof(context.SourceEvent.EventType));

        if (IsEventType(eventType, AuditEventTypes.MediaAssetRegistered))
        {
            return NormalizeAssetRegistered(context);
        }

        if (IsEventType(eventType, AuditEventTypes.MediaAssetUpdated))
        {
            return NormalizeAssetUpdated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.MediaAssetSoftDeleted))
        {
            return NormalizeAssetSoftDeleted(context);
        }

        if (IsEventType(eventType, AuditEventTypes.MediaAssetRestored))
        {
            return NormalizeAssetRestored(context);
        }

        if (IsEventType(eventType, AuditEventTypes.MediaArticleMediaAttached))
        {
            return NormalizeArticleMediaAttached(context);
        }

        if (IsEventType(eventType, AuditEventTypes.MediaArticleMediaDetached))
        {
            return NormalizeArticleMediaDetached(context);
        }

        if (IsEventType(eventType, AuditEventTypes.MediaArticleMediaReordered))
        {
            return NormalizeArticleMediaReordered(context);
        }

        if (IsEventType(eventType, AuditEventTypes.MediaArticlePrimaryMediaSet))
        {
            return NormalizeArticlePrimaryMediaSet(context);
        }

        throw new InvalidOperationException(
            $"Unsupported media audit event type '{context.SourceEvent.EventType}'.");
    }

    private AuditNormalizedEvent NormalizeAssetRegistered(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<MediaAssetRegisteredAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildMediaAssetResource(
                payload.MediaPublicId,
                payload.FileName,
                payload.AltText,
                payload.MediaType),
            normalizedPayload: new
            {
                mediaInternalId = payload.MediaId,
                mediaPublicId = NormalizeRequired(payload.MediaPublicId, nameof(payload.MediaPublicId)),
                storageProvider = NormalizeRequired(payload.StorageProvider, nameof(payload.StorageProvider)),
                fileName = NormalizeOptional(payload.FileName),
                mediaType = NormalizeRequired(payload.MediaType, nameof(payload.MediaType)),
                mimeType = NormalizeOptional(payload.MimeType),
                fileSizeBytes = payload.FileSizeBytes,
                width = payload.Width,
                height = payload.Height,
                durationSeconds = payload.DurationSeconds,
                altText = NormalizeOptional(payload.AltText),
                actorInternalId = payload.ActorUserId,
                version = payload.Version,
                registeredAtUtc = payload.RegisteredAtUtc
            },
            summary: "Media asset was registered.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeAssetUpdated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<MediaAssetUpdatedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildMediaAssetResource(
                payload.MediaPublicId,
                fileName: null,
                altText: payload.AltText,
                mediaType: null),
            normalizedPayload: new
            {
                mediaInternalId = payload.MediaId,
                mediaPublicId = NormalizeRequired(payload.MediaPublicId, nameof(payload.MediaPublicId)),
                altText = NormalizeOptional(payload.AltText),
                actorInternalId = payload.ActorUserId,
                version = payload.Version,
                updatedAtUtc = payload.UpdatedAtUtc
            },
            summary: "Media asset was updated.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeAssetSoftDeleted(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<MediaAssetSoftDeletedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildMediaAssetResource(
                payload.MediaPublicId,
                fileName: null,
                altText: null,
                mediaType: null),
            normalizedPayload: new
            {
                mediaInternalId = payload.MediaId,
                mediaPublicId = NormalizeRequired(payload.MediaPublicId, nameof(payload.MediaPublicId)),
                isDeleted = payload.IsDeleted,
                restoreUntil = payload.RestoreUntil,
                primaryClearedCount = payload.PrimaryClearedCount,
                actorInternalId = payload.ActorUserId,
                version = payload.Version,
                deletedAtUtc = payload.DeletedAtUtc
            },
            summary: "Media asset was soft deleted.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeAssetRestored(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<MediaAssetRestoredAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildMediaAssetResource(
                payload.MediaPublicId,
                fileName: null,
                altText: null,
                mediaType: null),
            normalizedPayload: new
            {
                mediaInternalId = payload.MediaId,
                mediaPublicId = NormalizeRequired(payload.MediaPublicId, nameof(payload.MediaPublicId)),
                isDeleted = payload.IsDeleted,
                actorInternalId = payload.ActorUserId,
                version = payload.Version,
                restoredAtUtc = payload.RestoredAtUtc
            },
            summary: "Media asset was restored.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeArticleMediaAttached(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleMediaAttachedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleMediaSetResource(articleInternalId: payload.ArticleId),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                mediaInternalId = payload.MediaId,
                mediaPublicId = NormalizeRequired(payload.MediaPublicId, nameof(payload.MediaPublicId)),
                articleMediaInternalId = payload.ArticleMediaId,
                mediaType = NormalizeRequired(payload.MediaType, nameof(payload.MediaType)),
                altText = NormalizeOptional(payload.AltText),
                altTextOverride = NormalizeOptional(payload.AltTextOverride),
                effectiveAltText = NormalizeOptional(payload.EffectiveAltText),
                caption = NormalizeOptional(payload.Caption),
                sortOrder = payload.SortOrder,
                isPrimary = payload.IsPrimary,
                primaryChanged = payload.PrimaryChanged,
                actorInternalId = payload.ActorUserId,
                attachmentSetVersion = payload.AttachmentSetVersion,
                attachedAtUtc = payload.AttachedAtUtc
            },
            summary: "Media was attached to article.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeArticleMediaDetached(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleMediaDetachedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleMediaSetResource(articleInternalId: payload.ArticleId),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                mediaInternalId = payload.MediaId,
                primaryCleared = payload.PrimaryCleared,
                actorInternalId = payload.ActorUserId,
                attachmentSetVersion = payload.AttachmentSetVersion,
                detachedAtUtc = payload.DetachedAtUtc
            },
            summary: "Media was detached from article.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeArticleMediaReordered(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticleMediaReorderedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleMediaSetResource(articleInternalId: payload.ArticleId),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                items = payload.Items.Select(item => new
                {
                    mediaInternalId = item.MediaId,
                    sortOrder = item.SortOrder
                }),
                actorInternalId = payload.ActorUserId,
                attachmentSetVersion = payload.AttachmentSetVersion,
                reorderedAtUtc = payload.ReorderedAtUtc
            },
            summary: "Article media was reordered.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeArticlePrimaryMediaSet(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<ArticlePrimaryMediaSetAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(actorInternalId: payload.ActorUserId),
            resource: BuildArticleMediaSetResource(articleInternalId: payload.ArticleId),
            normalizedPayload: new
            {
                articleInternalId = payload.ArticleId,
                mediaInternalId = payload.MediaId,
                mediaPublicId = NormalizeRequired(payload.MediaPublicId, nameof(payload.MediaPublicId)),
                mediaType = NormalizeRequired(payload.MediaType, nameof(payload.MediaType)),
                altText = NormalizeOptional(payload.AltText),
                altTextOverride = NormalizeOptional(payload.AltTextOverride),
                effectiveAltText = NormalizeOptional(payload.EffectiveAltText),
                caption = NormalizeOptional(payload.Caption),
                sortOrder = payload.SortOrder,
                actorInternalId = payload.ActorUserId,
                attachmentSetVersion = payload.AttachmentSetVersion,
                primarySetAtUtc = payload.PrimarySetAtUtc
            },
            summary: "Article primary media was set.",
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

    private static AuditResource BuildMediaAssetResource(
        string mediaPublicId,
        string? fileName,
        string? altText,
        string? mediaType)
    {
        var normalizedMediaPublicId = NormalizeRequired(
            mediaPublicId,
            nameof(mediaPublicId));

        return AuditResource.Create(
            AuditResourceTypes.MediaAsset,
            normalizedMediaPublicId,
            ResolveDisplayName(fileName, altText, mediaType, normalizedMediaPublicId));
    }

    private static AuditResource BuildArticleMediaSetResource(
        long articleInternalId)
    {
        return AuditResource.Create(
            AuditResourceTypes.ArticleMediaSet,
            articleInternalId.ToString(),
            $"Article {articleInternalId} media set");
    }

    private static string ResolveDisplayName(
        string? primary,
        string? secondary,
        string? tertiary,
        string fallback)
    {
        return NormalizeOptional(primary) ??
            NormalizeOptional(secondary) ??
            NormalizeOptional(tertiary) ??
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
                $"Media audit payload field '{parameterName}' is required.");
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
