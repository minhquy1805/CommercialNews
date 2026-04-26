namespace Authorization.Domain.Constants;

public static class DefaultRolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> All =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemRoles.Admin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionKeys.Content.Articles.Create,
                PermissionKeys.Content.Articles.Read,
                PermissionKeys.Content.Articles.Update,
                PermissionKeys.Content.Articles.Publish,
                PermissionKeys.Content.Articles.Unpublish,
                PermissionKeys.Content.Articles.Archive,
                PermissionKeys.Content.Articles.Restore,
                PermissionKeys.Content.Articles.Delete,
                PermissionKeys.Content.Articles.ReadRevisions,

                PermissionKeys.Content.Categories.Create,
                PermissionKeys.Content.Categories.Read,
                PermissionKeys.Content.Categories.Update,
                PermissionKeys.Content.Categories.Delete,
                PermissionKeys.Content.Categories.Restore,

                PermissionKeys.Content.Tags.Create,
                PermissionKeys.Content.Tags.Read,
                PermissionKeys.Content.Tags.Update,
                PermissionKeys.Content.Tags.Delete,
                PermissionKeys.Content.Tags.Restore,

                PermissionKeys.Seo.Metadata.Create,
                PermissionKeys.Seo.Metadata.Read,
                PermissionKeys.Seo.Metadata.Update,

                PermissionKeys.Seo.SlugRoutes.Create,
                PermissionKeys.Seo.SlugRoutes.Read,
                PermissionKeys.Seo.SlugRoutes.Update,
                PermissionKeys.Seo.SlugRoutes.Activate,
                PermissionKeys.Seo.SlugRoutes.Deactivate,
                PermissionKeys.Seo.SlugRoutes.Generate,

                PermissionKeys.Authz.Permissions.Read,
                PermissionKeys.Authz.Permissions.Create,
                PermissionKeys.Authz.Permissions.Update,
                PermissionKeys.Authz.Permissions.Activate,
                PermissionKeys.Authz.Permissions.Deactivate,

                PermissionKeys.Authz.Roles.Read,
                PermissionKeys.Authz.Roles.Create,
                PermissionKeys.Authz.Roles.Update,
                PermissionKeys.Authz.Roles.Activate,
                PermissionKeys.Authz.Roles.Deactivate,

                PermissionKeys.Authz.RolePermissions.Read,
                PermissionKeys.Authz.RolePermissions.Grant,
                PermissionKeys.Authz.RolePermissions.Revoke,

                PermissionKeys.Authz.UserRoles.Read,
                PermissionKeys.Authz.UserRoles.Assign,
                PermissionKeys.Authz.UserRoles.Revoke,
                PermissionKeys.Authz.UserPermissions.ReadEffective
            },

            [SystemRoles.Moderator] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionKeys.Content.Articles.Read,
                PermissionKeys.Content.Articles.Update,
                PermissionKeys.Content.Articles.Unpublish,
                PermissionKeys.Content.Articles.Archive,
                PermissionKeys.Content.Articles.Restore,
                PermissionKeys.Content.Articles.ReadRevisions,

                PermissionKeys.Content.Categories.Read,
                PermissionKeys.Content.Tags.Read,

                PermissionKeys.Seo.Metadata.Read,
                PermissionKeys.Seo.SlugRoutes.Read
            },

            [SystemRoles.Author] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionKeys.Content.Articles.Create,
                PermissionKeys.Content.Articles.Read,
                PermissionKeys.Content.Articles.Update,
                PermissionKeys.Content.Articles.ReadRevisions,

                PermissionKeys.Content.Categories.Read,
                PermissionKeys.Content.Tags.Read,

                PermissionKeys.Seo.Metadata.Read,
                PermissionKeys.Seo.Metadata.Update,
                PermissionKeys.Seo.SlugRoutes.Read,
                PermissionKeys.Seo.SlugRoutes.Generate
            },

            [SystemRoles.User] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
            }
        };

    public static IReadOnlySet<string> GetPermissionsForRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Empty;
        }

        return All.TryGetValue(roleName.Trim(), out var permissions)
            ? permissions
            : Empty;
    }

    private static readonly IReadOnlySet<string> Empty =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}