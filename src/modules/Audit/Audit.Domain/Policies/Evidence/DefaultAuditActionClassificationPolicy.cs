using Audit.Domain.Constants.Common;
using Audit.Domain.Constants.Events;
using System.Collections.Frozen;

namespace Audit.Domain.Policies.Evidence;

public sealed class DefaultAuditActionClassificationPolicy : IAuditActionClassificationPolicy
{
    private static readonly FrozenDictionary<string, ActionMapping> EventMappings =
        new ActionMapping[]
        {
            new(AuditEventTypes.AuthorizationUserRoleAssigned, "UserRoleAssigned", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationUserRoleRevoked, "UserRoleRevoked", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationRolePermissionGranted, "RolePermissionGranted", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationRolePermissionRevoked, "RolePermissionRevoked", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationRoleCreated, "RoleCreated", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationRoleUpdated, "RoleUpdated", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationRoleActivated, "RoleActivated", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationRoleDeactivated, "RoleDeactivated", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationPermissionCreated, "PermissionCreated", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationPermissionUpdated, "PermissionUpdated", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationPermissionActivated, "PermissionActivated", AuditActionCategories.Authorization),
            new(AuditEventTypes.AuthorizationPermissionDeactivated, "PermissionDeactivated", AuditActionCategories.Authorization),

            new(AuditEventTypes.IdentityEmailVerified, "EmailVerified", AuditActionCategories.IdentitySecurity),
            new(AuditEventTypes.IdentityPasswordChanged, "PasswordChanged", AuditActionCategories.IdentitySecurity),
            new(AuditEventTypes.IdentityUserActivated, "UserActivated", AuditActionCategories.IdentitySecurity),
            new(AuditEventTypes.IdentityUserDisabled, "UserDisabled", AuditActionCategories.IdentitySecurity),
            new(AuditEventTypes.IdentityUserLocked, "UserLocked", AuditActionCategories.IdentitySecurity),
            new(AuditEventTypes.IdentityUserUnlocked, "UserUnlocked", AuditActionCategories.IdentitySecurity),
            new(AuditEventTypes.IdentityEmailMarkedVerified, "EmailMarkedVerified", AuditActionCategories.IdentitySecurity),
            new(AuditEventTypes.IdentityUserSessionsRevoked, "UserSessionsRevoked", AuditActionCategories.IdentitySecurity),

            new(AuditEventTypes.ContentArticleCreated, "ArticleCreated", AuditActionCategories.ContentLifecycle),
            new(AuditEventTypes.ContentArticleUpdated, "ArticleUpdated", AuditActionCategories.ContentLifecycle),
            new(AuditEventTypes.ContentArticlePublished, "ArticlePublished", AuditActionCategories.ContentLifecycle),
            new(AuditEventTypes.ContentArticleUnpublished, "ArticleUnpublished", AuditActionCategories.ContentLifecycle),
            new(AuditEventTypes.ContentArticleArchived, "ArticleArchived", AuditActionCategories.ContentLifecycle),
            new(AuditEventTypes.ContentArticleSoftDeleted, "ArticleSoftDeleted", AuditActionCategories.ContentLifecycle),

            new(AuditEventTypes.MediaAssetRegistered, "MediaAssetRegistered", AuditActionCategories.MediaGovernance),
            new(AuditEventTypes.MediaAssetUpdated, "MediaAssetUpdated", AuditActionCategories.MediaGovernance),
            new(AuditEventTypes.MediaAssetSoftDeleted, "MediaAssetSoftDeleted", AuditActionCategories.MediaGovernance),
            new(AuditEventTypes.MediaAssetRestored, "MediaAssetRestored", AuditActionCategories.MediaGovernance),
            new(AuditEventTypes.MediaArticleMediaAttached, "MediaAttachedToArticle", AuditActionCategories.MediaGovernance),
            new(AuditEventTypes.MediaArticleMediaDetached, "MediaDetachedFromArticle", AuditActionCategories.MediaGovernance),
            new(AuditEventTypes.MediaArticleMediaReordered, "ArticleMediaReordered", AuditActionCategories.MediaGovernance),
            new(AuditEventTypes.MediaArticlePrimaryMediaSet, "ArticlePrimaryMediaSet", AuditActionCategories.MediaGovernance),

            new(AuditEventTypes.InteractionCommentHidden, "CommentHidden", AuditActionCategories.Moderation),
            new(AuditEventTypes.InteractionCommentRestored, "CommentRestored", AuditActionCategories.Moderation),
            new(AuditEventTypes.InteractionCommentDeletedByAuthor, "CommentDeletedByAuthor", AuditActionCategories.Moderation),
            new(AuditEventTypes.InteractionCommentReportsDismissed, "CommentReportsDismissed", AuditActionCategories.Moderation)
        }
        .ToFrozenDictionary(
            mapping => mapping.EventType,
            StringComparer.OrdinalIgnoreCase);

    public AuditActionClassificationResult Classify(
        string sourceModule,
        string eventType)
    {
        var normalizedEventType = eventType?.Trim();
        if (EventMappings.TryGetValue(normalizedEventType ?? string.Empty, out var mapping))
        {
            return AuditActionClassificationResult.Create(
                mapping.Action,
                mapping.ActionCategory);
        }

        return AuditActionClassificationResult.Create(
            BuildFallbackAction(normalizedEventType),
            ResolveCategory(sourceModule));
    }

    private static string? ResolveCategory(string? sourceModule)
    {
        var normalizedSourceModule = sourceModule?.Trim();

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Authorization))
        {
            return AuditActionCategories.Authorization;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Identity))
        {
            return AuditActionCategories.IdentitySecurity;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Content))
        {
            return AuditActionCategories.ContentLifecycle;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Media))
        {
            return AuditActionCategories.MediaGovernance;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Interaction))
        {
            return AuditActionCategories.Moderation;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Seo))
        {
            return AuditActionCategories.SeoGovernance;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Notifications))
        {
            return AuditActionCategories.NotificationDelivery;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.Audit))
        {
            return AuditActionCategories.AuditIngestion;
        }

        if (EqualsOrdinalIgnoreCase(normalizedSourceModule, AuditSourceModules.System))
        {
            return AuditActionCategories.System;
        }

        return null;
    }

    private static string BuildFallbackAction(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return "AuditEvent";
        }

        var tokens = eventType
            .Split(new[] { '.', '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToPascalToken)
            .Where(token => token.Length > 0);

        var action = string.Concat(tokens);
        if (string.IsNullOrWhiteSpace(action))
        {
            return "AuditEvent";
        }

        return action.Length <= AuditConstants.MaxActionLength
            ? action
            : action[..AuditConstants.MaxActionLength];
    }

    private static string ToPascalToken(string value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (value.Length == 1)
        {
            return value.ToUpperInvariant();
        }

        return string.Concat(
            value[..1].ToUpperInvariant(),
            value[1..].ToLowerInvariant());
    }

    private static bool EqualsOrdinalIgnoreCase(string? left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ActionMapping(
        string EventType,
        string Action,
        string ActionCategory);
}
