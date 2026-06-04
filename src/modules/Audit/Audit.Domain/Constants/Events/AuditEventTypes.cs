using System.Collections.Frozen;

namespace Audit.Domain.Constants.Events;

public static class AuditEventTypes
{
    // Authorization - UserRole
    public const string AuthorizationUserRoleAssigned = "authorization.user_role_assigned";
    public const string AuthorizationUserRoleRevoked = "authorization.user_role_revoked";

    // Authorization - RolePermission
    public const string AuthorizationRolePermissionGranted = "authorization.role_permission_granted";
    public const string AuthorizationRolePermissionRevoked = "authorization.role_permission_revoked";

    // Authorization - Role
    public const string AuthorizationRoleCreated = "authorization.role_created";
    public const string AuthorizationRoleUpdated = "authorization.role_updated";
    public const string AuthorizationRoleActivated = "authorization.role_activated";
    public const string AuthorizationRoleDeactivated = "authorization.role_deactivated";

    // Authorization - Permission
    public const string AuthorizationPermissionCreated = "authorization.permission_created";
    public const string AuthorizationPermissionUpdated = "authorization.permission_updated";
    public const string AuthorizationPermissionActivated = "authorization.permission_activated";
    public const string AuthorizationPermissionDeactivated = "authorization.permission_deactivated";

    // Identity
    public const string IdentityEmailVerified = "identity.email_verified";
    public const string IdentityPasswordChanged = "identity.password_changed";

    public const string IdentityUserActivated = "identity.user_activated";
    public const string IdentityUserDisabled = "identity.user_disabled";
    public const string IdentityUserLocked = "identity.user_locked";
    public const string IdentityUserUnlocked = "identity.user_unlocked";

    public const string IdentityEmailMarkedVerified = "identity.email_marked_verified";
    public const string IdentityUserSessionsRevoked = "identity.user_sessions_revoked";

    // Content
    public const string ContentArticleCreated = "content.article_created";
    public const string ContentArticleUpdated = "content.article_updated";
    public const string ContentArticlePublished = "content.article_published";
    public const string ContentArticleUnpublished = "content.article_unpublished";
    public const string ContentArticleArchived = "content.article_archived";
    public const string ContentArticleSoftDeleted = "content.article_soft_deleted";

    // Media - Asset
    public const string MediaAssetRegistered = "media.asset_registered";
    public const string MediaAssetUpdated = "media.asset_updated";
    public const string MediaAssetSoftDeleted = "media.asset_soft_deleted";
    public const string MediaAssetRestored = "media.asset_restored";

    // Media - ArticleMedia
    public const string MediaArticleMediaAttached = "media.article_media_attached";
    public const string MediaArticleMediaDetached = "media.article_media_detached";
    public const string MediaArticleMediaReordered = "media.article_media_reordered";
    public const string MediaArticlePrimaryMediaSet = "media.article_primary_media_set";

    // Interaction
    public const string InteractionCommentHidden = "interaction.comment_hidden";
    public const string InteractionCommentRestored = "interaction.comment_restored";
    public const string InteractionCommentDeletedByAuthor = "interaction.comment_deleted_by_author";
    public const string InteractionCommentReportsDismissed = "interaction.comment_reports_dismissed";

    public static readonly FrozenSet<string> All = new[]
    {
        AuthorizationUserRoleAssigned,
        AuthorizationUserRoleRevoked,

        AuthorizationRolePermissionGranted,
        AuthorizationRolePermissionRevoked,

        AuthorizationRoleCreated,
        AuthorizationRoleUpdated,
        AuthorizationRoleActivated,
        AuthorizationRoleDeactivated,

        AuthorizationPermissionCreated,
        AuthorizationPermissionUpdated,
        AuthorizationPermissionActivated,
        AuthorizationPermissionDeactivated,

        IdentityEmailVerified,
        IdentityPasswordChanged,

        IdentityUserActivated,
        IdentityUserDisabled,
        IdentityUserLocked,
        IdentityUserUnlocked,

        IdentityEmailMarkedVerified,
        IdentityUserSessionsRevoked,

        ContentArticleCreated,
        ContentArticleUpdated,
        ContentArticlePublished,
        ContentArticleUnpublished,
        ContentArticleArchived,
        ContentArticleSoftDeleted,

        MediaAssetRegistered,
        MediaAssetUpdated,
        MediaAssetSoftDeleted,
        MediaAssetRestored,

        MediaArticleMediaAttached,
        MediaArticleMediaDetached,
        MediaArticleMediaReordered,
        MediaArticlePrimaryMediaSet,

        InteractionCommentHidden,
        InteractionCommentRestored,
        InteractionCommentDeletedByAuthor,
        InteractionCommentReportsDismissed
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> AuthorizationEvents = new[]
    {
        AuthorizationUserRoleAssigned,
        AuthorizationUserRoleRevoked,

        AuthorizationRolePermissionGranted,
        AuthorizationRolePermissionRevoked,

        AuthorizationRoleCreated,
        AuthorizationRoleUpdated,
        AuthorizationRoleActivated,
        AuthorizationRoleDeactivated,

        AuthorizationPermissionCreated,
        AuthorizationPermissionUpdated,
        AuthorizationPermissionActivated,
        AuthorizationPermissionDeactivated
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> IdentityEvents = new[]
    {
        IdentityEmailVerified,
        IdentityPasswordChanged,

        IdentityUserActivated,
        IdentityUserDisabled,
        IdentityUserLocked,
        IdentityUserUnlocked,

        IdentityEmailMarkedVerified,
        IdentityUserSessionsRevoked
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> ContentEvents = new[]
    {
        ContentArticleCreated,
        ContentArticleUpdated,
        ContentArticlePublished,
        ContentArticleUnpublished,
        ContentArticleArchived,
        ContentArticleSoftDeleted
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> MediaEvents = new[]
    {
        MediaAssetRegistered,
        MediaAssetUpdated,
        MediaAssetSoftDeleted,
        MediaAssetRestored,

        MediaArticleMediaAttached,
        MediaArticleMediaDetached,
        MediaArticleMediaReordered,
        MediaArticlePrimaryMediaSet
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> InteractionEvents = new[]
    {
        InteractionCommentHidden,
        InteractionCommentRestored,
        InteractionCommentDeletedByAuthor,
        InteractionCommentReportsDismissed
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }

    public static bool IsAuthorizationEvent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return AuthorizationEvents.Contains(value);
    }

    public static bool IsIdentityEvent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return IdentityEvents.Contains(value);
    }

    public static bool IsContentEvent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return ContentEvents.Contains(value);
    }

    public static bool IsMediaEvent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return MediaEvents.Contains(value);
    }

    public static bool IsInteractionEvent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return InteractionEvents.Contains(value);
    }
}