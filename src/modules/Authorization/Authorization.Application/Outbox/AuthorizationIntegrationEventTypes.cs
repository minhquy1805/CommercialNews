namespace Authorization.Application.Outbox;

public static class AuthorizationIntegrationEventTypes
{
    public const string UserRoleAssigned = "authorization.user_role_assigned";
    public const string UserRoleRevoked = "authorization.user_role_revoked";

    public const string RolePermissionGranted = "authorization.role_permission_granted";
    public const string RolePermissionRevoked = "authorization.role_permission_revoked";

    public const string RoleCreated = "authorization.role_created";
    public const string RoleUpdated = "authorization.role_updated";
    public const string RoleActivated = "authorization.role_activated";
    public const string RoleDeactivated = "authorization.role_deactivated";

    public const string PermissionCreated = "authorization.permission_created";
    public const string PermissionUpdated = "authorization.permission_updated";
    public const string PermissionActivated = "authorization.permission_activated";
    public const string PermissionDeactivated = "authorization.permission_deactivated";
}