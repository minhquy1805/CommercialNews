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

    public const string ContentArticlesCreate = "Permission:content:articles:create";
    public const string ContentArticlesRead = "Permission:content:articles:read";
    public const string ContentArticlesUpdate = "Permission:content:articles:update";
    public const string ContentArticlesPublish = "Permission:content:articles:publish";
    public const string ContentArticlesUnpublish = "Permission:content:articles:unpublish";
    public const string ContentArticlesArchive = "Permission:content:articles:archive";
    public const string ContentArticlesRestore = "Permission:content:articles:restore";
    public const string ContentArticlesDelete = "Permission:content:articles:delete";
    public const string ContentArticlesReadRevisions = "Permission:content:articles:read-revisions";

    public const string ContentCategoriesCreate = "Permission:content:categories:create";
    public const string ContentCategoriesRead = "Permission:content:categories:read";
    public const string ContentCategoriesUpdate = "Permission:content:categories:update";
    public const string ContentCategoriesDelete = "Permission:content:categories:delete";
    public const string ContentCategoriesRestore = "Permission:content:categories:restore";

    public const string ContentTagsCreate = "Permission:content:tags:create";
    public const string ContentTagsRead = "Permission:content:tags:read";
    public const string ContentTagsUpdate = "Permission:content:tags:update";
    public const string ContentTagsDelete = "Permission:content:tags:delete";
    public const string ContentTagsRestore = "Permission:content:tags:restore";

    public const string SeoMetadataCreate = "Permission:seo:metadata:create";
    public const string SeoMetadataRead = "Permission:seo:metadata:read";
    public const string SeoMetadataUpdate = "Permission:seo:metadata:update";

    public const string SeoSlugRoutesCreate = "Permission:seo:slug-routes:create";
    public const string SeoSlugRoutesRead = "Permission:seo:slug-routes:read";
    public const string SeoSlugRoutesUpdate = "Permission:seo:slug-routes:update";
    public const string SeoSlugRoutesActivate = "Permission:seo:slug-routes:activate";
    public const string SeoSlugRoutesDeactivate = "Permission:seo:slug-routes:deactivate";
    public const string SeoSlugRoutesGenerate = "Permission:seo:slug-routes:generate";
}