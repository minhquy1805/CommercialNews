using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Exceptions;

public sealed class IdentitySqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),

            547 => MapConstraintViolation(exception),

            51210 => new IdentityPersistenceException(
                code: "IDENTITY.REFRESH_TOKEN_NOT_FOUND",
                message: "Active refresh token was not found or is no longer valid.",
                innerException: exception),

            51211 => new IdentityPersistenceException(
                code: "IDENTITY.EMAIL_VERIFICATION_TOKEN_NOT_FOUND",
                message: "Active email verification token was not found or is no longer valid.",
                innerException: exception),

            51212 => new IdentityPersistenceException(
                code: "IDENTITY.PASSWORD_RESET_TOKEN_NOT_FOUND",
                message: "Active password reset token was not found or is no longer valid.",
                innerException: exception),

            51213 => new IdentityPersistenceException(
                code: "IDENTITY.USER_ACCOUNT_CREATED_TIME_RANGE_INVALID",
                message: "User account created time range is invalid.",
                innerException: exception),

            51214 => new IdentityPersistenceException(
                code: "IDENTITY.USER_ACCOUNT_STATUS_FILTER_INVALID",
                message: "User account status filter is invalid.",
                innerException: exception),

            51215 => new IdentityPersistenceException(
                code: "IDENTITY.USER_LOCKED_UNTIL_REQUIRED",
                message: "LockedUntil is required.",
                innerException: exception),

            51216 => new IdentityPersistenceException(
                code: "IDENTITY.USER_LOCKED_UNTIL_MUST_BE_IN_FUTURE",
                message: "LockedUntil must be in the future.",
                innerException: exception),

            _ => new IdentityPersistenceException(
                code: "IDENTITY.PERSISTENCE_ERROR",
                message: "An unexpected SQL persistence error occurred.",
                innerException: exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_UserAccount_EmailNormalized", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.EMAIL_EXISTS",
                message: "An account with this email already exists.",
                innerException: exception);
        }

        if (message.Contains("UQ_UserAccount_PublicId", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.USER_PUBLIC_ID_DUPLICATE",
                message: "User public id already exists.",
                innerException: exception);
        }

        if (message.Contains("UQ_EmailVerificationToken_TokenHash", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.EMAIL_VERIFICATION_TOKEN_HASH_DUPLICATE",
                message: "Email verification token hash already exists.",
                innerException: exception);
        }

        if (message.Contains("UQ_PasswordResetToken_TokenHash", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.PASSWORD_RESET_TOKEN_HASH_DUPLICATE",
                message: "Password reset token hash already exists.",
                innerException: exception);
        }

        if (message.Contains("UQ_RefreshToken_TokenHash", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.REFRESH_TOKEN_HASH_DUPLICATE",
                message: "Refresh token hash already exists.",
                innerException: exception);
        }

        return new IdentityPersistenceException(
            code: "IDENTITY.PERSISTENCE_CONSTRAINT_VIOLATION",
            message: "A persistence constraint was violated.",
            innerException: exception);
    }

    private static Exception MapConstraintViolation(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("FK_EmailVerificationToken_UserAccount", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.EMAIL_VERIFICATION_USER_NOT_FOUND",
                message: "Email verification token references a user account that does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_PasswordResetToken_UserAccount", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.PASSWORD_RESET_USER_NOT_FOUND",
                message: "Password reset token references a user account that does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_RefreshToken_UserAccount", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.REFRESH_TOKEN_USER_NOT_FOUND",
                message: "Refresh token references a user account that does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_LoginHistory_UserAccount", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.LOGIN_HISTORY_USER_NOT_FOUND",
                message: "Login history references a user account that does not exist.",
                innerException: exception);
        }

        if (message.Contains("CK_UserAccount_Status", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.USER_ACCOUNT_STATUS_INVALID",
                message: "User account status is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_UserAccount_Version", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.USER_ACCOUNT_VERSION_INVALID",
                message: "User account version is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_UserAccount_EmailVerifiedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.USER_ACCOUNT_EMAIL_VERIFIED_AT_INVALID",
                message: "EmailVerifiedAt cannot be earlier than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_UserAccount_LockedUntil", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.USER_ACCOUNT_LOCKED_UNTIL_INVALID",
                message: "LockedUntil cannot be earlier than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailVerificationToken_ExpiresAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.EMAIL_VERIFICATION_EXPIRES_AT_INVALID",
                message: "Email verification token ExpiresAt must be greater than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailVerificationToken_UsedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.EMAIL_VERIFICATION_USED_AT_INVALID",
                message: "Email verification token UsedAt cannot be earlier than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_PasswordResetToken_ExpiresAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.PASSWORD_RESET_EXPIRES_AT_INVALID",
                message: "Password reset token ExpiresAt must be greater than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_PasswordResetToken_UsedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.PASSWORD_RESET_USED_AT_INVALID",
                message: "Password reset token UsedAt cannot be earlier than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_PasswordResetToken_RevokedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.PASSWORD_RESET_REVOKED_AT_INVALID",
                message: "Password reset token RevokedAt cannot be earlier than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_RefreshToken_ExpiresAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.REFRESH_TOKEN_EXPIRES_AT_INVALID",
                message: "Refresh token ExpiresAt must be greater than CreatedAt.",
                innerException: exception);
        }

        if (message.Contains("CK_RefreshToken_RevokedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new IdentityPersistenceException(
                code: "IDENTITY.REFRESH_TOKEN_REVOKED_AT_INVALID",
                message: "Refresh token RevokedAt cannot be earlier than CreatedAt.",
                innerException: exception);
        }

        return new IdentityPersistenceException(
            code: "IDENTITY.PERSISTENCE_CONSTRAINT_VIOLATION",
            message: "A persistence constraint was violated.",
            innerException: exception);
    }
}