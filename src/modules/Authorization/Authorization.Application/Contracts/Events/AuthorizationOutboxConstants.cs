namespace Authorization.Application.Contracts.Events
{
    public static class AuthorizationOutboxConstants
    {
        public static class EventTypes
        {
            public const string UserRoleAssigned = "Authorization.UserRoleAssigned";
            public const string UserRoleRevoked = "Authorization.UserRoleRevoked";
            public const string RolePermissionGranted = "Authorization.RolePermissionGranted";
            public const string RolePermissionRevoked = "Authorization.RolePermissionRevoked";

            public const string RoleCreated = "Authorization.RoleCreated";
            public const string RoleUpdated = "Authorization.RoleUpdated";
            public const string RoleDeactivated = "Authorization.RoleDeactivated";

            public const string PermissionCreated = "Authorization.PermissionCreated";
            public const string PermissionUpdated = "Authorization.PermissionUpdated";
            public const string PermissionDeactivated = "Authorization.PermissionDeactivated";

            public const string RoleActivated = "Authorization.RoleActivated";
            public const string PermissionActivated = "Authorization.PermissionActivated";
        }

        public static class AggregateTypes
        {
            public const string UserRole = "UserRole";
            public const string RolePermission = "RolePermission";
            public const string Role = "Role";
            public const string Permission = "Permission";
        }
    }
}