using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Errors;

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

    public static readonly Error AuthenticationRequired =
        Error.Unauthorized(
            code: "AUTHORIZATION.AUTHENTICATION_REQUIRED",
            message: "Authentication is required.");

    public static readonly Error FailClosed =
        Error.Forbidden(
            code: "AUTHORIZATION.FAIL_CLOSED",
            message: "Authorization could not be evaluated safely and was denied.");

    public static readonly Error DependencyFailure =
        Error.Failure(
            code: "AUTHORIZATION.DEPENDENCY_FAILURE",
            message: "A required authorization dependency failed.");

    public static readonly Error UnexpectedError =
        Error.Failure(
            code: "AUTHORIZATION.UNEXPECTED_ERROR",
            message: "An unexpected authorization error occurred.");

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
                message: "Role name must not exceed 80 characters.");

        public static readonly Error NameNormalizedRequired =
            Error.Validation(
                code: "AUTHORIZATION.ROLE_NAME_NORMALIZED_REQUIRED",
                message: "Normalized role name is required.");

        public static readonly Error NameNormalizedTooLong =
            Error.Validation(
                code: "AUTHORIZATION.ROLE_NAME_NORMALIZED_TOO_LONG",
                message: "Normalized role name must not exceed 80 characters.");

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

        public static readonly Error DisplayNameTooLong =
            Error.Validation(
                code: "AUTHORIZATION.ROLE_DISPLAY_NAME_TOO_LONG",
                message: "Role display name must not exceed 120 characters.");

        public static readonly Error DescriptionTooLong =
            Error.Validation(
                code: "AUTHORIZATION.ROLE_DESCRIPTION_TOO_LONG",
                message: "Role description must not exceed 300 characters.");
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
                message: "Permission key already exists.");

        public static readonly Error PublicIdRequired =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_PUBLIC_ID_REQUIRED",
                message: "Permission public id is required.");

        public static readonly Error KeyRequired =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_KEY_REQUIRED",
                message: "Permission key is required.");

        public static readonly Error KeyTooLong =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_KEY_TOO_LONG",
                message: "Permission key must not exceed 120 characters.");

        public static readonly Error KeyNormalizedRequired =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_KEY_NORMALIZED_REQUIRED",
                message: "Normalized permission key is required.");

        public static readonly Error KeyNormalizedTooLong =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_KEY_NORMALIZED_TOO_LONG",
                message: "Normalized permission key must not exceed 120 characters.");

        public static readonly Error ModuleTooLong =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_MODULE_TOO_LONG",
                message: "Permission module must not exceed 50 characters.");

        public static readonly Error ActionTooLong =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_ACTION_TOO_LONG",
                message: "Permission action must not exceed 50 characters.");

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

        public static readonly Error DescriptionTooLong =
            Error.Validation(
                code: "AUTHORIZATION.PERMISSION_DESCRIPTION_TOO_LONG",
                message: "Permission description must not exceed 300 characters.");
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
        public static readonly Error NotFound =
            Error.NotFound(
                code: "AUTHORIZATION.USER_ROLE_NOT_FOUND",
                message: "User role assignment was not found.");

        public static readonly Error AlreadyAssigned =
            Error.Conflict(
                code: "AUTHORIZATION.USER_ROLE_ALREADY_ASSIGNED",
                message: "Role is already assigned to the user.");

        public static readonly Error InvalidUserId =
            Error.Validation(
                code: "AUTHORIZATION.USER_ROLE_INVALID_USER_ID",
                message: "User id must be greater than zero.");

        public static readonly Error InvalidRoleId =
            Error.Validation(
                code: "AUTHORIZATION.USER_ROLE_INVALID_ROLE_ID",
                message: "Role id must be greater than zero.");

        public static readonly Error InvalidAssignTime =
            Error.Validation(
                code: "AUTHORIZATION.USER_ROLE_INVALID_ASSIGN_TIME",
                message: "Assigned time is invalid.");
    }

    public static class RolePermission
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "AUTHORIZATION.ROLE_PERMISSION_NOT_FOUND",
                message: "Role permission grant was not found.");

        public static readonly Error AlreadyGranted =
            Error.Conflict(
                code: "AUTHORIZATION.ROLE_PERMISSION_ALREADY_GRANTED",
                message: "Permission is already granted to the role.");

        public static readonly Error InvalidRoleId =
            Error.Validation(
                code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_ID",
                message: "Role id must be greater than zero.");

        public static readonly Error InvalidPermissionId =
            Error.Validation(
                code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_PERMISSION_ID",
                message: "Permission id must be greater than zero.");

        public static readonly Error InvalidGrantTime =
            Error.Validation(
                code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_GRANT_TIME",
                message: "Granted time is invalid.");
    }

    public static class Query
    {
        public static readonly Error InvalidPaging =
            Error.Validation(
                code: "AUTHORIZATION.INVALID_PAGING",
                message: "Paging parameters are invalid.");

        public static readonly Error InvalidSortField =
            Error.Validation(
                code: "AUTHORIZATION.INVALID_SORT_FIELD",
                message: "The requested sort field is not supported.");

        public static readonly Error InvalidFilter =
            Error.Validation(
                code: "AUTHORIZATION.INVALID_FILTER",
                message: "One or more filters are invalid.");
    }

    public static class Concurrency
    {
        public static readonly Error Conflict =
            Error.Conflict(
                code: "AUTHORIZATION.CONCURRENCY_CONFLICT",
                message: "The authorization resource was modified by another operation.");
    }
}