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
}