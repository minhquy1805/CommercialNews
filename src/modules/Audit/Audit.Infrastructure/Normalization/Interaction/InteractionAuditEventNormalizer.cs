using Audit.Application.Abstractions.Serialization;
using Audit.Application.Services.Normalization;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Events;
using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Evidence;
using Audit.Infrastructure.Normalization.Common;
using Audit.Infrastructure.Normalization.Interaction.EventPayloads;

namespace Audit.Infrastructure.Normalization.Interaction;

internal sealed class InteractionAuditEventNormalizer : AuditNormalizerBase
{
    public override string SourceModule => AuditSourceModules.Interaction;

    public InteractionAuditEventNormalizer(
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
        return AuditEventTypes.IsInteractionEvent(eventType);
    }

    protected override AuditNormalizedEvent NormalizeCore(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var eventType = NormalizeRequired(
            context.SourceEvent.EventType,
            nameof(context.SourceEvent.EventType));

        if (IsEventType(eventType, AuditEventTypes.InteractionCommentHidden))
        {
            return NormalizeCommentHidden(context);
        }

        if (IsEventType(eventType, AuditEventTypes.InteractionCommentRestored))
        {
            return NormalizeCommentRestored(context);
        }

        if (IsEventType(eventType, AuditEventTypes.InteractionCommentDeletedByAuthor))
        {
            return NormalizeCommentDeletedByAuthor(context);
        }

        if (IsEventType(eventType, AuditEventTypes.InteractionCommentReportsDismissed))
        {
            return NormalizeCommentReportsDismissed(context);
        }

        throw new InvalidOperationException(
            $"Unsupported interaction audit event type '{context.SourceEvent.EventType}'.");
    }

    private AuditNormalizedEvent NormalizeCommentHidden(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<CommentHiddenAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildModeratorActor(moderatorInternalId: payload.ModeratorUserId),
            resource: BuildCommentResource(payload.CommentPublicId),
            normalizedPayload: new
            {
                commentPublicId = NormalizeRequired(payload.CommentPublicId, nameof(payload.CommentPublicId)),
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                resolutionSource = NormalizeRequired(payload.ResolutionSource, nameof(payload.ResolutionSource)),
                commentModerationCasePublicId = NormalizeOptional(payload.CommentModerationCasePublicId),
                resolvedReportCount = payload.ResolvedReportCount,
                reasonCode = NormalizeRequired(payload.ReasonCode, nameof(payload.ReasonCode)),
                moderatorInternalId = payload.ModeratorUserId,
                hiddenAtUtc = payload.HiddenAtUtc
            },
            summary: "Comment was hidden.",
            reason: payload.ReasonCode);
    }

    private AuditNormalizedEvent NormalizeCommentRestored(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<CommentRestoredAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildModeratorActor(moderatorInternalId: payload.ModeratorUserId),
            resource: BuildCommentResource(payload.CommentPublicId),
            normalizedPayload: new
            {
                commentPublicId = NormalizeRequired(payload.CommentPublicId, nameof(payload.CommentPublicId)),
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                moderatorInternalId = payload.ModeratorUserId,
                restoredAtUtc = payload.RestoredAtUtc
            },
            summary: "Comment was restored.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeCommentDeletedByAuthor(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<CommentDeletedByAuthorAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildUserActor(authorInternalId: payload.AuthorUserId),
            resource: BuildCommentResource(payload.CommentPublicId),
            normalizedPayload: new
            {
                commentPublicId = NormalizeRequired(payload.CommentPublicId, nameof(payload.CommentPublicId)),
                articlePublicId = NormalizeRequired(payload.ArticlePublicId, nameof(payload.ArticlePublicId)),
                authorInternalId = payload.AuthorUserId,
                wasVisible = payload.WasVisible,
                closedOpenCase = payload.ClosedOpenCase,
                deletedAtUtc = payload.DeletedAtUtc
            },
            summary: "Comment was deleted by author.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizeCommentReportsDismissed(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<CommentReportsDismissedAuditPayload>(context);

        return CreateNormalizedEvent(
            context: context,
            actor: BuildModeratorActor(moderatorInternalId: payload.ModeratorUserId),
            resource: AuditResource.Create(
                AuditResourceTypes.CommentModerationCase,
                NormalizeRequired(
                    payload.CommentModerationCasePublicId,
                    nameof(payload.CommentModerationCasePublicId)),
                $"Comment moderation case {payload.CommentModerationCasePublicId}"),
            normalizedPayload: new
            {
                commentModerationCasePublicId = NormalizeRequired(
                    payload.CommentModerationCasePublicId,
                    nameof(payload.CommentModerationCasePublicId)),
                caseStatus = NormalizeRequired(payload.CaseStatus, nameof(payload.CaseStatus)),
                reasonCode = NormalizeRequired(payload.ReasonCode, nameof(payload.ReasonCode)),
                moderatorInternalId = payload.ModeratorUserId,
                resolvedAtUtc = payload.ResolvedAtUtc
            },
            summary: "Comment reports were dismissed.",
            reason: payload.ReasonCode);
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

    private static AuditActor BuildModeratorActor(
        long moderatorInternalId)
    {
        return AuditActor.Create(
            actorInternalId: moderatorInternalId,
            actorUserId: null,
            actorEmail: null,
            actorDisplayName: null,
            actorType: AuditActorTypes.Moderator);
    }

    private static AuditActor BuildUserActor(
        long authorInternalId)
    {
        return AuditActor.Create(
            actorInternalId: authorInternalId,
            actorUserId: null,
            actorEmail: null,
            actorDisplayName: null,
            actorType: AuditActorTypes.User);
    }

    private static AuditResource BuildCommentResource(
        string commentPublicId)
    {
        var normalizedCommentPublicId = NormalizeRequired(
            commentPublicId,
            nameof(commentPublicId));

        return AuditResource.Create(
            AuditResourceTypes.Comment,
            normalizedCommentPublicId,
            $"Comment {normalizedCommentPublicId}");
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
                $"Interaction audit payload field '{parameterName}' is required.");
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
