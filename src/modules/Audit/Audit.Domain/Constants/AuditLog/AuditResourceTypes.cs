using System.Collections.Frozen;

namespace Audit.Domain.Constants.AuditLog;

public static class AuditResourceTypes
{
    // Authorization
    public const string Role = "Role";
    public const string Permission = "Permission";
    public const string UserRole = "UserRole";
    public const string RolePermission = "RolePermission";

    // Identity
    public const string UserAccount = "UserAccount";
    public const string LoginHistory = "LoginHistory";
    public const string RefreshToken = "RefreshToken";
    public const string EmailVerificationToken = "EmailVerificationToken";
    public const string PasswordResetToken = "PasswordResetToken";

    // Content
    public const string Article = "Article";
    public const string ArticleRevision = "ArticleRevision";
    public const string ArticleLifecycleEvent = "ArticleLifecycleEvent";
    public const string ArticleTag = "ArticleTag";
    public const string Category = "Category";
    public const string Tag = "Tag";

    // Media
    public const string MediaAsset = "MediaAsset";
    public const string MediaVariant = "MediaVariant";
    public const string ArticleMedia = "ArticleMedia";
    public const string ArticleMediaSet = "ArticleMediaSet";

    // Interaction
    public const string Comment = "Comment";
    public const string CommentReport = "CommentReport";
    public const string CommentModerationCase = "CommentModerationCase";
    public const string CommentModerationActionHistory = "CommentModerationActionHistory";
    public const string ArticleLike = "ArticleLike";
    public const string ArticleViewCount = "ArticleViewCount";
    public const string ArticleInteractionStats = "ArticleInteractionStats";
    public const string ArticleInteractionTargetProjection = "ArticleInteractionTargetProjection";

    // SEO / Notifications future coverage
    public const string SeoMetadata = "SeoMetadata";
    public const string SlugRegistry = "SlugRegistry";
    public const string SeoRoute = "SeoRoute";
    public const string EmailDelivery = "EmailDelivery";
    public const string EmailDeliveryAttempt = "EmailDeliveryAttempt";

    public static readonly FrozenSet<string> All = new[]
    {
        Role,
        Permission,
        UserRole,
        RolePermission,

        UserAccount,
        LoginHistory,
        RefreshToken,
        EmailVerificationToken,
        PasswordResetToken,

        Article,
        ArticleRevision,
        ArticleLifecycleEvent,
        ArticleTag,
        Category,
        Tag,

        MediaAsset,
        MediaVariant,
        ArticleMedia,
        ArticleMediaSet,

        Comment,
        CommentReport,
        CommentModerationCase,
        CommentModerationActionHistory,
        ArticleLike,
        ArticleViewCount,
        ArticleInteractionStats,
        ArticleInteractionTargetProjection,

        SeoMetadata,
        SlugRegistry,
        SeoRoute,
        EmailDelivery,
        EmailDeliveryAttempt
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> CurrentAuditBaseline = new[]
    {
        Role,
        Permission,
        UserRole,
        RolePermission,

        UserAccount,

        Article,

        MediaAsset,
        ArticleMedia,
        ArticleMediaSet,

        Comment,
        CommentReport,
        CommentModerationCase
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }

    public static bool IsCurrentAuditBaseline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return CurrentAuditBaseline.Contains(value);
    }
}
