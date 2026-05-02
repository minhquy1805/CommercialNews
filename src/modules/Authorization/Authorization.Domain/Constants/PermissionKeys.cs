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
            public const string Restore = "content:articles:restore";
            public const string Delete = "content:articles:delete";
            public const string ReadRevisions = "content:articles:read-revisions";
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

    public static class Seo
    {
        public static class Metadata
        {
            public const string Create = "seo:metadata:create";
            public const string Read = "seo:metadata:read";
            public const string Update = "seo:metadata:update";
        }

        public static class SlugRoutes
        {
            public const string Create = "seo:slug-routes:create";
            public const string Read = "seo:slug-routes:read";
            public const string Update = "seo:slug-routes:update";
            public const string Activate = "seo:slug-routes:activate";
            public const string Deactivate = "seo:slug-routes:deactivate";
            public const string Generate = "seo:slug-routes:generate";
        }
    }

    public static class Audit
    {
        public static class Logs
        {
            public const string Read = "audit:logs:read";
            public const string ReadDetail = "audit:logs:read-detail";
            public const string ReadByCorrelation = "audit:logs:read-by-correlation";
            public const string ReadByEvent = "audit:logs:read-by-event";
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