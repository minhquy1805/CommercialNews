namespace Authorization.Domain.Constants;

public static class PermissionKeys
{
    public static class Content
    {
        public static class Articles
        {
            public const string Create = "content:articles:create";
            public const string Read = "content:articles:read";
            public const string Update = "content:articles:update";
            public const string Publish = "content:articles:publish";
            public const string Unpublish = "content:articles:unpublish";
            public const string Archive = "content:articles:archive";
            public const string Delete = "content:articles:delete";
            public const string ReadRevisions = "content:articles:read-revisions";
            public const string ReadLifecycleEvents = "content:articles:read-lifecycle-events";
            public const string ReadTags = "content:articles:read-tags";
        }

        public static class Categories
        {
            public const string Create = "content:categories:create";
            public const string Read = "content:categories:read";
            public const string Update = "content:categories:update";
            public const string Delete = "content:categories:delete";
            public const string Restore = "content:categories:restore";
        }

        public static class Tags
        {
            public const string Create = "content:tags:create";
            public const string Read = "content:tags:read";
            public const string Update = "content:tags:update";
            public const string Delete = "content:tags:delete";
            public const string Restore = "content:tags:restore";
        }
    }

    public static class Media
    {
        public static class Assets
        {
            public const string Create = "media:assets:create";
            public const string Read = "media:assets:read";
            public const string Update = "media:assets:update";
            public const string Delete = "media:assets:delete";
            public const string Restore = "media:assets:restore";
            public const string ReadUsage = "media:assets:read-usage";
        }

        public static class ArticleMedia
        {
            public const string Read = "media:article-media:read";
            public const string Attach = "media:article-media:attach";
            public const string Detach = "media:article-media:detach";
            public const string SetPrimary = "media:article-media:set-primary";
            public const string Reorder = "media:article-media:reorder";
            public const string ReadState = "media:article-media:read-state";
        }
    }

    public static class Seo
    {
        public static class Metadata
        {
            public const string Read = "seo:metadata:read";
            public const string Update = "seo:metadata:update";
        }

        public static class ArticleSettings
        {
            public const string Read = Metadata.Read;
            public const string Upsert = Metadata.Update;
        }

        public static class SlugRoutes
        {
            public const string Read = "seo:slug-routes:read";
            public const string Generate = "seo:slug-routes:generate";

            public const string Create = "seo:slug-routes:create";
            public const string Update = "seo:slug-routes:update";
            public const string Activate = "seo:slug-routes:activate";
            public const string Deactivate = "seo:slug-routes:deactivate";
        }

        public static class SlugAvailability
        {
            public const string Read = SlugRoutes.Read;
        }

        public static class SlugGeneration
        {
            public const string Generate = SlugRoutes.Generate;
        }
    }

    public static class Audit
    {
        public static class Modules
        {
            public const string Read = "audit:modules:read";
        }

        public static class Logs
        {
            public const string Read = "audit:logs:read";
            public const string ReadDetail = "audit:logs:read-detail";
            public const string ReadByCorrelation = "audit:logs:read-by-correlation";
            public const string ReadByMessage = "audit:logs:read-by-message";
        }

        public static class Ingestion
        {
            public const string Read = "audit:ingestion:read";
            public const string ReadDetail = "audit:ingestion:read-detail";
        }

        public static class Dashboard
        {
            public const string Read = "audit:dashboard:read";
        }
    }

    public static class Identity
    {
        public static class Users
        {
            public const string Read = "identity:users:read";
            public const string ReadSecurity = "identity:users:read-security";
            public const string ManageStatus = "identity:users:manage-status";
            public const string ManageSecurity = "identity:users:manage-security";
            public const string VerifyEmail = "identity:users:verify-email";
            public const string RevokeSessions = "identity:users:revoke-sessions";
        }
    }

    public static class Interaction
    {
        public static class Comments
        {
            public const string Read = "interaction:comments:read";
            public const string Moderate = "interaction:comments:moderate";
        }

        public static class CommentReports
        {
            public const string Read = "interaction:comment-reports:read";
            public const string Resolve = "interaction:comment-reports:resolve";
        }

        public static class Counters
        {
            public const string Read = "interaction:counters:read";
        }
    }

    public static class Authz
    {
        public static class Permissions
        {
            public const string Read = "authz:permissions:read";
            public const string Create = "authz:permissions:create";
            public const string Update = "authz:permissions:update";
            public const string Activate = "authz:permissions:activate";
            public const string Deactivate = "authz:permissions:deactivate";
        }

        public static class Roles
        {
            public const string Read = "authz:roles:read";
            public const string Create = "authz:roles:create";
            public const string Update = "authz:roles:update";
            public const string Activate = "authz:roles:activate";
            public const string Deactivate = "authz:roles:deactivate";
        }

        public static class RolePermissions
        {
            public const string Read = "authz:role-permissions:read";
            public const string Grant = "authz:role-permissions:grant";
            public const string Revoke = "authz:role-permissions:revoke";
        }

        public static class UserRoles
        {
            public const string Read = "authz:user-roles:read";
            public const string Assign = "authz:user-roles:assign";
            public const string Revoke = "authz:user-roles:revoke";
        }

        public static class UserPermissions
        {
            public const string ReadEffective = "authz:user-permissions:read-effective";
        }
    }
}
