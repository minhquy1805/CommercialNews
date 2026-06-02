using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Events;
using Audit.Domain.ValueObjects.Evidence;
using System.Collections.Frozen;

namespace Audit.Domain.Policies.Evidence;

public sealed class DefaultAuditRiskClassificationPolicy : IAuditRiskClassificationPolicy
{
    private static readonly FrozenDictionary<string, RiskMapping> EventRules =
        new RiskMapping[]
        {
            new(AuditEventTypes.AuthorizationUserRoleAssigned, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-user-role-assigned", true),
            new(AuditEventTypes.AuthorizationUserRoleRevoked, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-user-role-revoked", true),
            new(AuditEventTypes.AuthorizationRolePermissionGranted, AuditSeverities.Warning, AuditRiskLevels.Critical, "authorization-role-permission-granted", true),
            new(AuditEventTypes.AuthorizationRolePermissionRevoked, AuditSeverities.Warning, AuditRiskLevels.Critical, "authorization-role-permission-revoked", true),
            new(AuditEventTypes.AuthorizationRoleCreated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-role-created", true),
            new(AuditEventTypes.AuthorizationRoleUpdated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-role-updated", true),
            new(AuditEventTypes.AuthorizationRoleActivated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-role-activated", true),
            new(AuditEventTypes.AuthorizationRoleDeactivated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-role-deactivated", true),
            new(AuditEventTypes.AuthorizationPermissionCreated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-permission-created", true),
            new(AuditEventTypes.AuthorizationPermissionUpdated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-permission-updated", true),
            new(AuditEventTypes.AuthorizationPermissionActivated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-permission-activated", true),
            new(AuditEventTypes.AuthorizationPermissionDeactivated, AuditSeverities.Warning, AuditRiskLevels.High, "authorization-permission-deactivated", true),

            new(AuditEventTypes.IdentityEmailVerified, AuditSeverities.Info, AuditRiskLevels.Low, "identity-email-verified", false),
            new(AuditEventTypes.IdentityPasswordChanged, AuditSeverities.Warning, AuditRiskLevels.High, "identity-password-changed", true),
            new(AuditEventTypes.IdentityUserActivated, AuditSeverities.Warning, AuditRiskLevels.Medium, "identity-user-activated", false),
            new(AuditEventTypes.IdentityUserDisabled, AuditSeverities.Warning, AuditRiskLevels.High, "identity-user-disabled", true),
            new(AuditEventTypes.IdentityUserLocked, AuditSeverities.Warning, AuditRiskLevels.High, "identity-user-locked", true),
            new(AuditEventTypes.IdentityUserUnlocked, AuditSeverities.Warning, AuditRiskLevels.High, "identity-user-unlocked", true),
            new(AuditEventTypes.IdentityEmailMarkedVerified, AuditSeverities.Warning, AuditRiskLevels.Medium, "identity-email-marked-verified", false),
            new(AuditEventTypes.IdentityUserSessionsRevoked, AuditSeverities.Warning, AuditRiskLevels.High, "identity-user-sessions-revoked", true),

            new(AuditEventTypes.ContentArticleCreated, AuditSeverities.Info, AuditRiskLevels.Low, "content-article-created", false),
            new(AuditEventTypes.ContentArticleUpdated, AuditSeverities.Info, AuditRiskLevels.Low, "content-article-updated", false),
            new(AuditEventTypes.ContentArticlePublished, AuditSeverities.Info, AuditRiskLevels.Medium, "content-article-published", false),
            new(AuditEventTypes.ContentArticleUnpublished, AuditSeverities.Info, AuditRiskLevels.Medium, "content-article-unpublished", false),
            new(AuditEventTypes.ContentArticleArchived, AuditSeverities.Info, AuditRiskLevels.Medium, "content-article-archived", false),
            new(AuditEventTypes.ContentArticleSoftDeleted, AuditSeverities.Info, AuditRiskLevels.Medium, "content-article-soft-deleted", false),

            new(AuditEventTypes.MediaAssetRegistered, AuditSeverities.Info, AuditRiskLevels.Low, "media-asset-registered", false),
            new(AuditEventTypes.MediaAssetUpdated, AuditSeverities.Info, AuditRiskLevels.Low, "media-asset-updated", false),
            new(AuditEventTypes.MediaAssetSoftDeleted, AuditSeverities.Info, AuditRiskLevels.Medium, "media-asset-soft-deleted", false),
            new(AuditEventTypes.MediaAssetRestored, AuditSeverities.Info, AuditRiskLevels.Medium, "media-asset-restored", false),
            new(AuditEventTypes.MediaArticleMediaAttached, AuditSeverities.Info, AuditRiskLevels.Medium, "media-article-media-attached", false),
            new(AuditEventTypes.MediaArticleMediaDetached, AuditSeverities.Info, AuditRiskLevels.Medium, "media-article-media-detached", false),
            new(AuditEventTypes.MediaArticleMediaReordered, AuditSeverities.Info, AuditRiskLevels.Medium, "media-article-media-reordered", false),
            new(AuditEventTypes.MediaArticlePrimaryMediaSet, AuditSeverities.Info, AuditRiskLevels.Medium, "media-article-primary-media-set", false),

            new(AuditEventTypes.InteractionCommentHidden, AuditSeverities.Warning, AuditRiskLevels.Medium, "interaction-comment-hidden", false),
            new(AuditEventTypes.InteractionCommentRestored, AuditSeverities.Warning, AuditRiskLevels.Medium, "interaction-comment-restored", false),
            new(AuditEventTypes.InteractionCommentDeletedByAuthor, AuditSeverities.Info, AuditRiskLevels.Low, "interaction-comment-deleted-by-author", false),
            new(AuditEventTypes.InteractionCommentReportsDismissed, AuditSeverities.Warning, AuditRiskLevels.Medium, "interaction-comment-reports-dismissed", false)
        }
        .ToFrozenDictionary(
            mapping => mapping.EventType,
            StringComparer.OrdinalIgnoreCase);

    public AuditRiskClassificationResult Classify(
        string sourceModule,
        string eventType,
        string action,
        string? actionCategory)
    {
        var normalizedEventType = eventType?.Trim();
        if (EventRules.TryGetValue(normalizedEventType ?? string.Empty, out var eventRule))
        {
            return CreateResult(eventRule.Rule);
        }

        return CreateResult(ResolveFallbackRule(sourceModule, actionCategory));
    }

    private static AuditRiskClassificationResult CreateResult(RiskRule rule)
    {
        return AuditRiskClassificationResult.Create(
            AuditRisk.Create(
                AuditOutcomes.Success,
                rule.Severity,
                rule.RiskLevel),
            rule.MatchedRule,
            rule.RequiresReview);
    }

    private static RiskRule ResolveFallbackRule(
        string? sourceModule,
        string? actionCategory)
    {
        if (EqualsOrdinalIgnoreCase(actionCategory, AuditActionCategories.Authorization) ||
            EqualsOrdinalIgnoreCase(sourceModule, AuditSourceModules.Authorization))
        {
            return new RiskRule(AuditSeverities.Warning, AuditRiskLevels.High, "fallback-authorization", true);
        }

        if (EqualsOrdinalIgnoreCase(actionCategory, AuditActionCategories.IdentitySecurity) ||
            EqualsOrdinalIgnoreCase(sourceModule, AuditSourceModules.Identity))
        {
            return new RiskRule(AuditSeverities.Warning, AuditRiskLevels.High, "fallback-identity-security", true);
        }

        if (EqualsOrdinalIgnoreCase(actionCategory, AuditActionCategories.Moderation) ||
            EqualsOrdinalIgnoreCase(sourceModule, AuditSourceModules.Interaction))
        {
            return new RiskRule(AuditSeverities.Warning, AuditRiskLevels.Medium, "fallback-moderation", false);
        }

        if (EqualsOrdinalIgnoreCase(actionCategory, AuditActionCategories.ContentLifecycle) ||
            EqualsOrdinalIgnoreCase(sourceModule, AuditSourceModules.Content))
        {
            return new RiskRule(AuditSeverities.Info, AuditRiskLevels.Medium, "fallback-content-lifecycle", false);
        }

        if (EqualsOrdinalIgnoreCase(actionCategory, AuditActionCategories.MediaGovernance) ||
            EqualsOrdinalIgnoreCase(sourceModule, AuditSourceModules.Media))
        {
            return new RiskRule(AuditSeverities.Info, AuditRiskLevels.Medium, "fallback-media-governance", false);
        }

        return new RiskRule(AuditSeverities.Info, AuditRiskLevels.Low, "fallback-default", false);
    }

    private static bool EqualsOrdinalIgnoreCase(string? left, string right)
    {
        return string.Equals(left?.Trim(), right, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RiskMapping(
        string EventType,
        string Severity,
        string RiskLevel,
        string MatchedRule,
        bool RequiresReview)
    {
        public RiskRule Rule { get; } = new(
            Severity,
            RiskLevel,
            MatchedRule,
            RequiresReview);
    }

    private sealed record RiskRule(
        string Severity,
        string RiskLevel,
        string MatchedRule,
        bool RequiresReview);
}
