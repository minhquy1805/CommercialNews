using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Exceptions
{
    public sealed class AuthorizationSqlExceptionTranslator : SqlExceptionTranslatorBase
    {
        public override Exception Translate(SqlException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.Number switch
            {
                // =========================================================
                // BOOTSTRAP / ENVIRONMENT
                // =========================================================
                53201 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.DATABASE_NOT_FOUND",
                    message: "Database [CommercialNews] does not exist.",
                    innerException: exception),

                53202 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.SCHEMA_NOT_FOUND",
                    message: "Schema [authorization] does not exist.",
                    innerException: exception),

                // =========================================================
                // ROLE
                // =========================================================
                53310 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_PUBLIC_ID_REQUIRED",
                    message: "Role public id is required.",
                    innerException: exception),

                53311 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_NAME_REQUIRED",
                    message: "Role name is required.",
                    innerException: exception),

                53312 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_NAME_NORMALIZED_REQUIRED",
                    message: "Normalized role name is required.",
                    innerException: exception),

                53313 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_EXISTS",
                    message: "Role name already exists.",
                    innerException: exception),

                53314 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_INVALID_ROLE_ID",
                    message: "Role id must be greater than zero.",
                    innerException: exception),

                53315 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.SYSTEM_ROLE_PROTECTED",
                    message: "System role is protected.",
                    innerException: exception),

                53316 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_NOT_FOUND",
                    message: "Role was not found.",
                    innerException: exception),

                // =========================================================
                // PERMISSION
                // =========================================================
                53330 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_PUBLIC_ID_REQUIRED",
                    message: "Permission public id is required.",
                    innerException: exception),

                53331 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_NAME_REQUIRED",
                    message: "Permission name is required.",
                    innerException: exception),

                53332 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_REQUIRED",
                    message: "Normalized permission name is required.",
                    innerException: exception),

                53333 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_EXISTS",
                    message: "Permission name already exists.",
                    innerException: exception),

                53334 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_INVALID_PERMISSION_ID",
                    message: "Permission id must be greater than zero.",
                    innerException: exception),

                53335 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.SYSTEM_PERMISSION_PROTECTED",
                    message: "System permission is protected.",
                    innerException: exception),

                53336 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_NOT_FOUND",
                    message: "Permission was not found.",
                    innerException: exception),

                // =========================================================
                // USER ROLE
                // =========================================================
                53352 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.USER_ROLE_INVALID_USER_ID",
                    message: "User id must be greater than zero.",
                    innerException: exception),

                53353 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.USER_ROLE_INVALID_ROLE_ID",
                    message: "Role id must be greater than zero.",
                    innerException: exception),

                // =========================================================
                // ROLE PERMISSION
                // =========================================================
                53372 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_ID",
                    message: "Role id must be greater than zero.",
                    innerException: exception),

                53373 => new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_PERMISSION_INVALID_PERMISSION_ID",
                    message: "Permission id must be greater than zero.",
                    innerException: exception),

                // =========================================================
                // SQL SERVER GENERIC CONSTRAINTS
                // =========================================================
                2601 or 2627 => TranslateDuplicateConstraint(exception),
                547 => TranslateConstraintConflict(exception),

                _ => exception
            };
        }

        private static Exception TranslateDuplicateConstraint(SqlException exception)
        {
            string message = exception.Message;

            if (ContainsAny(message, "UQ_Role_NameNormalized", "UQ_Role_Name"))
            {
                return new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_EXISTS",
                    message: "Role name already exists.",
                    innerException: exception);
            }

            if (ContainsAny(message, "UQ_Permission_NameNormalized", "UQ_Permission_Name"))
            {
                return new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_EXISTS",
                    message: "Permission name already exists.",
                    innerException: exception);
            }

            if (ContainsAny(message, "UX_UserRole_UserId_RoleId_Active"))
            {
                return new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.USER_ROLE_ALREADY_ASSIGNED",
                    message: "Role is already assigned to the user.",
                    innerException: exception);
            }

            if (ContainsAny(message, "UX_RolePermission_RoleId_PermissionId_Active"))
            {
                return new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_PERMISSION_ALREADY_GRANTED",
                    message: "Permission is already granted to the role.",
                    innerException: exception);
            }

            return new AuthorizationPersistenceException(
                code: "AUTHORIZATION.VALIDATION_FAILED",
                message: "A duplicate or uniqueness conflict occurred in authorization persistence.",
                innerException: exception);
        }

        private static Exception TranslateConstraintConflict(SqlException exception)
        {
            string message = exception.Message;

            if (ContainsAny(message, "[identity].[UserAccount]", "UserAccount"))
            {
                return new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.USER_NOT_FOUND",
                    message: "User was not found.",
                    innerException: exception);
            }

            if (ContainsAny(message, "[authorization].[Role]", "Role"))
            {
                return new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.ROLE_NOT_FOUND",
                    message: "Role was not found.",
                    innerException: exception);
            }

            if (ContainsAny(message, "[authorization].[Permission]", "Permission"))
            {
                return new AuthorizationPersistenceException(
                    code: "AUTHORIZATION.PERMISSION_NOT_FOUND",
                    message: "Permission was not found.",
                    innerException: exception);
            }

            return new AuthorizationPersistenceException(
                code: "AUTHORIZATION.VALIDATION_FAILED",
                message: "A constraint conflict occurred in authorization persistence.",
                innerException: exception);
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (value.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}