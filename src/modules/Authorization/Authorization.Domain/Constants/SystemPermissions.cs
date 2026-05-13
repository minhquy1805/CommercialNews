namespace Authorization.Domain.Constants;

public static class SystemPermissions
{
    public static readonly IReadOnlyList<SystemPermissionDefinition> All =
    [
        new(
            Key: PermissionKeys.Content.Articles.Create,
            Module: "Content",
            Action: "Create",
            Description: "Create articles"),

        new(
            Key: PermissionKeys.Content.Articles.Read,
            Module: "Content",
            Action: "Read",
            Description: "Read articles in admin content workflows"),

        new(
            Key: PermissionKeys.Content.Articles.Update,
            Module: "Content",
            Action: "Update",
            Description: "Update articles"),

        new(
            Key: PermissionKeys.Content.Articles.Publish,
            Module: "Content",
            Action: "Publish",
            Description: "Publish articles"),

        new(
            Key: PermissionKeys.Content.Articles.Unpublish,
            Module: "Content",
            Action: "Unpublish",
            Description: "Unpublish articles"),

        new(
            Key: PermissionKeys.Content.Articles.Archive,
            Module: "Content",
            Action: "Archive",
            Description: "Archive articles"),

        new(
            Key: PermissionKeys.Content.Articles.Delete,
            Module: "Content",
            Action: "Delete",
            Description: "Soft-delete articles"),

        new(
            Key: PermissionKeys.Content.Articles.ReadRevisions,
            Module: "Content",
            Action: "ReadRevisions",
            Description: "Read article revision history"),

        new(
            Key: PermissionKeys.Content.Articles.ReadLifecycleEvents,
            Module: "Content",
            Action: "ReadLifecycleEvents",
            Description: "Read article lifecycle event history"),

        new(
            Key: PermissionKeys.Content.Articles.ReadTags,
            Module: "Content",
            Action: "ReadTags",
            Description: "Read article tag assignments"),

        new(
            Key: PermissionKeys.Content.Categories.Create,
            Module: "Content",
            Action: "Create",
            Description: "Create categories"),

        new(
            Key: PermissionKeys.Content.Categories.Read,
            Module: "Content",
            Action: "Read",
            Description: "Read categories in admin content workflows"),

        new(
            Key: PermissionKeys.Content.Categories.Update,
            Module: "Content",
            Action: "Update",
            Description: "Update categories"),

        new(
            Key: PermissionKeys.Content.Categories.Delete,
            Module: "Content",
            Action: "Delete",
            Description: "Soft-delete categories"),

        new(
            Key: PermissionKeys.Content.Categories.Restore,
            Module: "Content",
            Action: "Restore",
            Description: "Restore deleted categories"),

        new(
            Key: PermissionKeys.Content.Tags.Create,
            Module: "Content",
            Action: "Create",
            Description: "Create tags"),

        new(
            Key: PermissionKeys.Content.Tags.Read,
            Module: "Content",
            Action: "Read",
            Description: "Read tags in admin content workflows"),

        new(
            Key: PermissionKeys.Content.Tags.Update,
            Module: "Content",
            Action: "Update",
            Description: "Update tags"),

        new(
            Key: PermissionKeys.Content.Tags.Delete,
            Module: "Content",
            Action: "Delete",
            Description: "Soft-delete tags"),

        new(
            Key: PermissionKeys.Content.Tags.Restore,
            Module: "Content",
            Action: "Restore",
            Description: "Restore deleted tags"),

        new(
            Key: PermissionKeys.Seo.Metadata.Create,
            Module: "Seo",
            Action: "Create",
            Description: "Create SEO metadata"),

        new(
            Key: PermissionKeys.Seo.Metadata.Read,
            Module: "Seo",
            Action: "Read",
            Description: "Read SEO metadata and article SEO settings"),

        new(
            Key: PermissionKeys.Seo.Metadata.Update,
            Module: "Seo",
            Action: "Update",
            Description: "Update SEO metadata and article SEO settings"),

        new(
            Key: PermissionKeys.Seo.SlugRoutes.Create,
            Module: "Seo",
            Action: "Create",
            Description: "Create slug routes"),

        new(
            Key: PermissionKeys.Seo.SlugRoutes.Read,
            Module: "Seo",
            Action: "Read",
            Description: "Read slug routes"),

        new(
            Key: PermissionKeys.Seo.SlugRoutes.Update,
            Module: "Seo",
            Action: "Update",
            Description: "Update slug routes"),

        new(
            Key: PermissionKeys.Seo.SlugRoutes.Activate,
            Module: "Seo",
            Action: "Activate",
            Description: "Activate slug routes"),

        new(
            Key: PermissionKeys.Seo.SlugRoutes.Deactivate,
            Module: "Seo",
            Action: "Deactivate",
            Description: "Deactivate slug routes"),

        new(
            Key: PermissionKeys.Seo.SlugRoutes.Generate,
            Module: "Seo",
            Action: "Generate",
            Description: "Generate slug suggestions"),

        new(
            Key: PermissionKeys.Identity.Users.Read,
            Module: "Identity",
            Action: "Read",
            Description: "Read Identity user accounts in admin workflows"),

        new(
            Key: PermissionKeys.Identity.Users.ReadSecurity,
            Module: "Identity",
            Action: "ReadSecurity",
            Description: "Read Identity user sessions and security summaries"),

        new(
            Key: PermissionKeys.Identity.Users.ManageStatus,
            Module: "Identity",
            Action: "ManageStatus",
            Description: "Activate or deactivate Identity user accounts"),

        new(
            Key: PermissionKeys.Identity.Users.ManageSecurity,
            Module: "Identity",
            Action: "ManageSecurity",
            Description: "Lock or unlock Identity user accounts"),

        new(
            Key: PermissionKeys.Identity.Users.VerifyEmail,
            Module: "Identity",
            Action: "VerifyEmail",
            Description: "Mark Identity user email addresses as verified"),

        new(
            Key: PermissionKeys.Identity.Users.RevokeSessions,
            Module: "Identity",
            Action: "RevokeSessions",
            Description: "Revoke Identity user refresh-token sessions"),

        new(
            Key: PermissionKeys.Authz.Permissions.Read,
            Module: "Authz",
            Action: "Read",
            Description: "Read permission definitions"),

        new(
            Key: PermissionKeys.Authz.Permissions.Create,
            Module: "Authz",
            Action: "Create",
            Description: "Create permission definitions"),

        new(
            Key: PermissionKeys.Authz.Permissions.Update,
            Module: "Authz",
            Action: "Update",
            Description: "Update permission definitions"),

        new(
            Key: PermissionKeys.Authz.Permissions.Activate,
            Module: "Authz",
            Action: "Activate",
            Description: "Activate permission definitions"),

        new(
            Key: PermissionKeys.Authz.Permissions.Deactivate,
            Module: "Authz",
            Action: "Deactivate",
            Description: "Deactivate permission definitions"),

        new(
            Key: PermissionKeys.Authz.Roles.Read,
            Module: "Authz",
            Action: "Read",
            Description: "Read roles"),

        new(
            Key: PermissionKeys.Authz.Roles.Create,
            Module: "Authz",
            Action: "Create",
            Description: "Create roles"),

        new(
            Key: PermissionKeys.Authz.Roles.Update,
            Module: "Authz",
            Action: "Update",
            Description: "Update roles"),

        new(
            Key: PermissionKeys.Authz.Roles.Activate,
            Module: "Authz",
            Action: "Activate",
            Description: "Activate roles"),

        new(
            Key: PermissionKeys.Authz.Roles.Deactivate,
            Module: "Authz",
            Action: "Deactivate",
            Description: "Deactivate roles"),

        new(
            Key: PermissionKeys.Authz.RolePermissions.Read,
            Module: "Authz",
            Action: "Read",
            Description: "Read role-permission mappings"),

        new(
            Key: PermissionKeys.Authz.RolePermissions.Grant,
            Module: "Authz",
            Action: "Grant",
            Description: "Grant permissions to roles"),

        new(
            Key: PermissionKeys.Authz.RolePermissions.Revoke,
            Module: "Authz",
            Action: "Revoke",
            Description: "Revoke permissions from roles"),

        new(
            Key: PermissionKeys.Authz.UserRoles.Read,
            Module: "Authz",
            Action: "Read",
            Description: "Read user-role assignments"),

        new(
            Key: PermissionKeys.Authz.UserRoles.Assign,
            Module: "Authz",
            Action: "Assign",
            Description: "Assign roles to users"),

        new(
            Key: PermissionKeys.Authz.UserRoles.Revoke,
            Module: "Authz",
            Action: "Revoke",
            Description: "Revoke roles from users"),

        new(
            Key: PermissionKeys.Authz.UserPermissions.ReadEffective,
            Module: "Authz",
            Action: "ReadEffective",
            Description: "Read effective permissions for a user"),

        new(
            Key: PermissionKeys.Audit.Logs.Read,
            Module: "Audit",
            Action: "Read",
            Description: "Read audit logs"),

        new(
            Key: PermissionKeys.Audit.Logs.ReadDetail,
            Module: "Audit",
            Action: "ReadDetail",
            Description: "Read audit log details"),

        new(
            Key: PermissionKeys.Audit.Logs.ReadByCorrelation,
            Module: "Audit",
            Action: "ReadByCorrelation",
            Description: "Read audit logs by correlation id"),

        new(
            Key: PermissionKeys.Audit.Logs.ReadByEvent,
            Module: "Audit",
            Action: "ReadByEvent",
            Description: "Read audit logs by audit event id")
    ];
}