namespace CommercialNews.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string AuthzRolesRead = "Permission:authz:roles:read";
    public const string AuthzRolesCreate = "Permission:authz:roles:create";
    public const string AuthzRolesUpdate = "Permission:authz:roles:update";
    public const string AuthzRolesActivate = "Permission:authz:roles:activate";
    public const string AuthzRolesDeactivate = "Permission:authz:roles:deactivate";

    public const string AuthzPermissionsRead = "Permission:authz:permissions:read";
    public const string AuthzPermissionsCreate = "Permission:authz:permissions:create";
    public const string AuthzPermissionsUpdate = "Permission:authz:permissions:update";
    public const string AuthzPermissionsActivate = "Permission:authz:permissions:activate";
    public const string AuthzPermissionsDeactivate = "Permission:authz:permissions:deactivate";

    public const string AuthzRolePermissionsRead = "Permission:authz:role-permissions:read";
    public const string AuthzRolePermissionsGrant = "Permission:authz:role-permissions:grant";
    public const string AuthzRolePermissionsRevoke = "Permission:authz:role-permissions:revoke";

    public const string AuthzUserRolesRead = "Permission:authz:user-roles:read";
    public const string AuthzUserRolesAssign = "Permission:authz:user-roles:assign";
    public const string AuthzUserRolesRevoke = "Permission:authz:user-roles:revoke";
    public const string AuthzUserPermissionsReadEffective = "Permission:authz:user-permissions:read-effective";

    public const string ContentArticlesCreate = "Content.Articles.Create";
    public const string ContentArticlesRead = "Content.Articles.Read";
    public const string ContentArticlesUpdate = "Content.Articles.Update";
    public const string ContentArticlesPublish = "Content.Articles.Publish";
    public const string ContentArticlesUnpublish = "Content.Articles.Unpublish";
    public const string ContentArticlesArchive = "Content.Articles.Archive";
    public const string ContentArticlesDelete = "Content.Articles.Delete";
    public const string ContentArticlesReadRevisions = "Content.Articles.ReadRevisions";
    public const string ContentArticlesReadLifecycleEvents = "Content.Articles.ReadLifecycleEvents";
    public const string ContentArticlesReadTags = "Content.Articles.ReadTags";

    public const string ContentCategoriesCreate = "Content.Categories.Create";
    public const string ContentCategoriesRead = "Content.Categories.Read";
    public const string ContentCategoriesUpdate = "Content.Categories.Update";
    public const string ContentCategoriesDelete = "Content.Categories.Delete";
    public const string ContentCategoriesRestore = "Content.Categories.Restore";

    public const string ContentTagsCreate = "Content.Tags.Create";
    public const string ContentTagsRead = "Content.Tags.Read";
    public const string ContentTagsUpdate = "Content.Tags.Update";
    public const string ContentTagsDelete = "Content.Tags.Delete";
    public const string ContentTagsRestore = "Content.Tags.Restore";

    public const string MediaAssetsCreate = "Permission:media:assets:create";
    public const string MediaAssetsRead = "Permission:media:assets:read";
    public const string MediaAssetsUpdate = "Permission:media:assets:update";
    public const string MediaAssetsDelete = "Permission:media:assets:delete";
    public const string MediaAssetsRestore = "Permission:media:assets:restore";
    public const string MediaAssetsReadUsage = "Permission:media:assets:read-usage";

    public const string MediaArticleMediaRead = "Permission:media:article-media:read";
    public const string MediaArticleMediaReadState = "Permission:media:article-media:read-state";
    public const string MediaArticleMediaAttach = "Permission:media:article-media:attach";
    public const string MediaArticleMediaDetach = "Permission:media:article-media:detach";
    public const string MediaArticleMediaSetPrimary = "Permission:media:article-media:set-primary";
    public const string MediaArticleMediaReorder = "Permission:media:article-media:reorder";

    public const string SeoMetadataRead = "Permission:seo:metadata:read";
    public const string SeoMetadataUpdate = "Permission:seo:metadata:update";
    public const string SeoArticleSettingsRead = SeoMetadataRead;
    public const string SeoArticleSettingsUpsert = SeoMetadataUpdate;

    public const string SeoSlugRoutesRead = "Permission:seo:slug-routes:read";
    public const string SeoSlugRoutesGenerate = "Permission:seo:slug-routes:generate";
    public const string SeoSlugAvailabilityRead = SeoSlugRoutesRead;
    public const string SeoSlugGenerate = SeoSlugRoutesGenerate;

    public const string AuditLogsRead = "Permission:audit:logs:read";
    public const string AuditLogsReadDetail = "Permission:audit:logs:read-detail";
    public const string AuditLogsReadByCorrelation = "Permission:audit:logs:read-by-correlation";
    public const string AuditLogsReadByEvent = "Permission:audit:logs:read-by-event";

    public const string IdentityUsersRead = "Permission:identity:users:read";
    public const string IdentityUsersReadSecurity = "Permission:identity:users:read-security";
    public const string IdentityUsersManageStatus = "Permission:identity:users:manage-status";
    public const string IdentityUsersManageSecurity = "Permission:identity:users:manage-security";
    public const string IdentityUsersVerifyEmail = "Permission:identity:users:verify-email";
    public const string IdentityUsersRevokeSessions = "Permission:identity:users:revoke-sessions";

    public const string InteractionCommentsRead = "Permission:interaction:comments:read";

    public const string InteractionCommentsModerate = "Permission:interaction:comments:moderate";

    public const string InteractionCommentReportsRead = "Permission:interaction:comment-reports:read";

    public const string InteractionCommentReportsResolve = "Permission:interaction:comment-reports:resolve";

    public const string InteractionCommentReportsHideComment = "Permission:interaction:comment-reports:hide-comment";

    public const string InteractionCountersRead = "Permission:interaction:counters:read";
}