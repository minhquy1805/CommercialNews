using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Errors
{
    public static class AuthorizationErrors
    {
        public static readonly Error ValidationFailed =
            Error.Validation(
                code: "AUTHORIZATION.VALIDATION_FAILED",
                message: "One or more authorization validations failed.");

        public static readonly Error PolicyDenied =
            Error.Forbidden(
                code: "AUTHORIZATION.POLICY_DENIED",
                message: "You do not have permission to perform this authorization action.");

        public static class Role
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "AUTHORIZATION.ROLE_NOT_FOUND",
                    message: "Role was not found.");

            public static readonly Error Exists =
                Error.Conflict(
                    code: "AUTHORIZATION.ROLE_EXISTS",
                    message: "Role name already exists.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_PUBLIC_ID_REQUIRED",
                    message: "Role public id is required.");

            public static readonly Error NameRequired =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_NAME_REQUIRED",
                    message: "Role name is required.");

            public static readonly Error NameTooLong =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_NAME_TOO_LONG",
                    message: "Role name must not exceed 100 characters.");

            public static readonly Error NameNormalizedRequired =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_NAME_NORMALIZED_REQUIRED",
                    message: "Normalized role name is required.");

            public static readonly Error NameNormalizedTooLong =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_NAME_NORMALIZED_TOO_LONG",
                    message: "Normalized role name must not exceed 100 characters.");

            public static readonly Error InvalidRoleId =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_INVALID_ROLE_ID",
                    message: "Role id must be greater than zero.");

            public static readonly Error InvalidTimestamp =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_INVALID_TIMESTAMP",
                    message: "Role timestamp is invalid.");

            public static readonly Error StaleUpdateTime =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_STALE_UPDATE_TIME",
                    message: "Role update time cannot be earlier than the current update time.");

            public static readonly Error SystemProtected =
                Error.Conflict(
                    code: "AUTHORIZATION.SYSTEM_ROLE_PROTECTED",
                    message: "System role is protected.");

            public static readonly Error Inactive =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_INACTIVE",
                    message: "Role is inactive.");
                    }

        public static class Permission
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "AUTHORIZATION.PERMISSION_NOT_FOUND",
                    message: "Permission was not found.");

            public static readonly Error Exists =
                Error.Conflict(
                    code: "AUTHORIZATION.PERMISSION_EXISTS",
                    message: "Permission name already exists.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_PUBLIC_ID_REQUIRED",
                    message: "Permission public id is required.");

            public static readonly Error NameRequired =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_NAME_REQUIRED",
                    message: "Permission name is required.");

            public static readonly Error NameTooLong =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_NAME_TOO_LONG",
                    message: "Permission name must not exceed 150 characters.");

            public static readonly Error NameNormalizedRequired =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_REQUIRED",
                    message: "Normalized permission name is required.");

            public static readonly Error NameNormalizedTooLong =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_TOO_LONG",
                    message: "Normalized permission name must not exceed 150 characters.");

            public static readonly Error ModuleTooLong =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_MODULE_TOO_LONG",
                    message: "Permission module must not exceed 100 characters.");

            public static readonly Error InvalidPermissionId =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_INVALID_PERMISSION_ID",
                    message: "Permission id must be greater than zero.");

            public static readonly Error InvalidTimestamp =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_INVALID_TIMESTAMP",
                    message: "Permission timestamp is invalid.");

            public static readonly Error StaleUpdateTime =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_STALE_UPDATE_TIME",
                    message: "Permission update time cannot be earlier than the current update time.");

            public static readonly Error SystemProtected =
                Error.Conflict(
                    code: "AUTHORIZATION.SYSTEM_PERMISSION_PROTECTED",
                    message: "System permission is protected.");

            public static readonly Error Inactive =
                Error.Validation(
                    code: "AUTHORIZATION.PERMISSION_INACTIVE",
                    message: "Permission is inactive.");
                    }

        public static class User
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "AUTHORIZATION.USER_NOT_FOUND",
                    message: "User was not found.");
        }

        public static class UserRole
        {
            public static readonly Error AlreadyAssigned =
                Error.Conflict(
                    code: "AUTHORIZATION.USER_ROLE_ALREADY_ASSIGNED",
                    message: "Role is already assigned to the user.");

            public static readonly Error AlreadyRevoked =
                Error.Validation(
                    code: "AUTHORIZATION.USER_ROLE_ALREADY_REVOKED",
                    message: "User role assignment is already revoked.");

            public static readonly Error InvalidUserRoleId =
                Error.Validation(
                    code: "AUTHORIZATION.USER_ROLE_INVALID_USER_ROLE_ID",
                    message: "User role id must be greater than zero.");

            public static readonly Error InvalidUserId =
                Error.Validation(
                    code: "AUTHORIZATION.USER_ROLE_INVALID_USER_ID",
                    message: "User id must be greater than zero.");

            public static readonly Error InvalidRoleId =
                Error.Validation(
                    code: "AUTHORIZATION.USER_ROLE_INVALID_ROLE_ID",
                    message: "Role id must be greater than zero.");

            public static readonly Error InvalidRevokeTime =
                Error.Validation(
                    code: "AUTHORIZATION.USER_ROLE_INVALID_REVOKE_TIME",
                    message: "Revoked time cannot be earlier than assigned time.");

            public static readonly Error InvalidRevokeState =
                Error.Validation(
                    code: "AUTHORIZATION.USER_ROLE_INVALID_REVOKE_STATE",
                    message: "Revocation state is invalid.");
        }

        public static class RolePermission
        {
            public static readonly Error AlreadyGranted =
                Error.Conflict(
                    code: "AUTHORIZATION.ROLE_PERMISSION_ALREADY_GRANTED",
                    message: "Permission is already granted to the role.");

            public static readonly Error AlreadyRevoked =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_PERMISSION_ALREADY_REVOKED",
                    message: "Role permission grant is already revoked.");

            public static readonly Error InvalidRolePermissionId =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_PERMISSION_ID",
                    message: "Role permission id must be greater than zero.");

            public static readonly Error InvalidRoleId =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_ID",
                    message: "Role id must be greater than zero.");

            public static readonly Error InvalidPermissionId =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_PERMISSION_ID",
                    message: "Permission id must be greater than zero.");

            public static readonly Error InvalidRevokeTime =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_REVOKE_TIME",
                    message: "Revoked time cannot be earlier than granted time.");

            public static readonly Error InvalidRevokeState =
                Error.Validation(
                    code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_REVOKE_STATE",
                    message: "Revocation state is invalid.");
        }
    }
}