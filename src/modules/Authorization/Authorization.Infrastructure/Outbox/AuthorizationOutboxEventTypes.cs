namespace Authorization.Infrastructure.Outbox;

internal static class AuthorizationOutboxEventTypes
{
    public const string RoleCreated = "Authz.RoleCreated";
    public const string RoleUpdated = "Authz.RoleUpdated";
    public const string RoleActivated = "Authz.RoleActivated";
    public const string RoleDeactivated = "Authz.RoleDeactivated";

    public const string PermissionCreated = "Authz.PermissionCreated";
    public const string PermissionUpdated = "Authz.PermissionUpdated";
    public const string PermissionActivated = "Authz.PermissionActivated";
    public const string PermissionDeactivated = "Authz.PermissionDeactivated";

    public const string UserRoleAssigned = "Authz.UserRoleAssigned";
    public const string UserRoleRevoked = "Authz.UserRoleRevoked";
    public const string RolePermissionGranted = "Authz.RolePermissionGranted";
    public const string RolePermissionRevoked = "Authz.RolePermissionRevoked";
}