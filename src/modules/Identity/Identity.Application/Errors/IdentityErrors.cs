using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Identity.Application.Errors
{
    public static class IdentityErrors
    {
        public static readonly Error ValidationFailed =
            Error.Validation(
                code: "IDENTITY.VALIDATION_FAILED",
                message: "One or more identity validations failed.");

        public static readonly Error InvalidCredentials =
            Error.Unauthorized(
                code: "IDENTITY.INVALID_CREDENTIALS",
                message: "Invalid credentials.");

        public static readonly Error AccountLocked =
            Error.Unauthorized(
                code: "IDENTITY.ACCOUNT_LOCKED",
                message: "The account is locked.");

        public static readonly Error PasswordPolicyViolation =
            Error.Validation(
                code: "IDENTITY.PASSWORD_POLICY_VIOLATION",
                message: "The new password does not satisfy the password policy.");

        public static readonly Error RefreshReuseDetected =
            Error.Unauthorized(
                code: "IDENTITY.REFRESH_REUSE_DETECTED",
                message: "Refresh token reuse was detected.");

        public static readonly Error RateLimited =
            Error.RateLimited(
                code: "IDENTITY.RATE_LIMITED",
                message: "Too many identity requests. Please try again later.");

        public static readonly Error EmailAlreadyExists =
            Error.Conflict(
                code: "IDENTITY.EMAIL_EXISTS",
                message: "An account with this email already exists.");

        public static class User
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "IDENTITY.USER_NOT_FOUND",
                    message: "User account was not found.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "IDENTITY.USER_PUBLIC_ID_REQUIRED",
                    message: "User public id is required.");

            public static readonly Error PublicIdInvalid =
                Error.Validation(
                    code: "IDENTITY.USER_PUBLIC_ID_INVALID",
                    message: "User public id must be exactly 26 characters.");

            public static readonly Error EmailRequired =
                Error.Validation(
                    code: "IDENTITY.USER_EMAIL_REQUIRED",
                    message: "Email is required.");

            public static readonly Error EmailTooLong =
                Error.Validation(
                    code: "IDENTITY.USER_EMAIL_TOO_LONG",
                    message: "Email must not exceed 320 characters.");

            public static readonly Error EmailNormalizedRequired =
                Error.Validation(
                    code: "IDENTITY.USER_EMAIL_NORMALIZED_REQUIRED",
                    message: "Normalized email is required.");

            public static readonly Error EmailNormalizedTooLong =
                Error.Validation(
                    code: "IDENTITY.USER_EMAIL_NORMALIZED_TOO_LONG",
                    message: "Normalized email must not exceed 320 characters.");

            public static readonly Error PasswordHashRequired =
                Error.Validation(
                    code: "IDENTITY.USER_PASSWORD_HASH_REQUIRED",
                    message: "Password hash is required.");

            public static readonly Error PasswordHashTooLong =
                Error.Validation(
                    code: "IDENTITY.USER_PASSWORD_HASH_TOO_LONG",
                    message: "Password hash must not exceed 500 characters.");

            public static readonly Error FullNameTooLong =
                Error.Validation(
                    code: "IDENTITY.USER_FULL_NAME_TOO_LONG",
                    message: "Full name must not exceed 200 characters.");

            public static readonly Error AvatarUrlTooLong =
                Error.Validation(
                    code: "IDENTITY.USER_AVATAR_URL_TOO_LONG",
                    message: "Avatar URL must not exceed 800 characters.");

            public static readonly Error InvalidUserId =
                Error.Validation(
                    code: "IDENTITY.USER_INVALID_USER_ID",
                    message: "User id must be greater than zero.");

            public static readonly Error InvalidVersion =
                Error.Validation(
                    code: "IDENTITY.USER_INVALID_VERSION",
                    message: "User version must be greater than or equal to 1.");

            public static readonly Error InvalidUpdatedAt =
                Error.Validation(
                    code: "IDENTITY.USER_INVALID_UPDATED_AT",
                    message: "Updated time cannot be earlier than created time.");

            public static readonly Error InvalidEmailVerifiedAt =
                Error.Validation(
                    code: "IDENTITY.USER_INVALID_EMAIL_VERIFIED_AT",
                    message: "Email verified time cannot be earlier than created time.");

            public static readonly Error InvalidLockedUntil =
                Error.Validation(
                    code: "IDENTITY.USER_INVALID_LOCKED_UNTIL",
                    message: "Locked-until time cannot be earlier than created time.");

            public static readonly Error InvalidLastLoginAt =
                Error.Validation(
                    code: "IDENTITY.USER_INVALID_LAST_LOGIN_AT",
                    message: "Last login time cannot be earlier than created time.");

            public static readonly Error AlreadyVerified =
                Error.Validation(
                    code: "IDENTITY.USER_ALREADY_VERIFIED",
                    message: "The user email is already verified.");

            public static readonly Error AlreadyActive =
                Error.Validation(
                    code: "IDENTITY.USER_ALREADY_ACTIVE",
                    message: "The user account is already active.");

            public static readonly Error AlreadyInactive =
                Error.Validation(
                    code: "IDENTITY.USER_ALREADY_INACTIVE",
                    message: "The user account is already inactive.");

            public static readonly Error AlreadyLocked =
                Error.Validation(
                    code: "IDENTITY.USER_ALREADY_LOCKED",
                    message: "The user account is already locked.");
        }

        public static class EmailVerification
        {
            public static readonly Error TokenNotFound =
                Error.NotFound(
                    code: "IDENTITY.EMAIL_VERIFICATION_TOKEN_NOT_FOUND",
                    message: "Email verification token was not found.");

            public static readonly Error TokenAlreadyUsed =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_TOKEN_ALREADY_USED",
                    message: "Email verification token has already been used.");

            public static readonly Error TokenExpired =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_TOKEN_EXPIRED",
                    message: "Email verification token has expired.");

            public static readonly Error TokenHashRequired =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_TOKEN_HASH_REQUIRED",
                    message: "Email verification token hash is required.");

            public static readonly Error TokenHashInvalid =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_TOKEN_HASH_INVALID",
                    message: "Email verification token hash is invalid.");

            public static readonly Error InvalidVerificationTokenId =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_INVALID_TOKEN_ID",
                    message: "Verification token id must be greater than zero.");

            public static readonly Error InvalidUserId =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_INVALID_USER_ID",
                    message: "User id must be greater than zero.");

            public static readonly Error InvalidExpiresAt =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_INVALID_EXPIRES_AT",
                    message: "Expiration time must be greater than created time.");

            public static readonly Error InvalidUsedAt =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_INVALID_USED_AT",
                    message: "Used time cannot be earlier than created time.");

            public static readonly Error CreatedIpTooLong =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_CREATED_IP_TOO_LONG",
                    message: "Created IP must not exceed 45 characters.");

            public static readonly Error CorrelationIdTooLong =
                Error.Validation(
                    code: "IDENTITY.EMAIL_VERIFICATION_CORRELATION_ID_TOO_LONG",
                    message: "Correlation id must not exceed 100 characters.");
        }

        public static class PasswordReset
        {
            public static readonly Error TokenNotFound =
                Error.NotFound(
                    code: "IDENTITY.PASSWORD_RESET_TOKEN_NOT_FOUND",
                    message: "Password reset token was not found.");

            public static readonly Error TokenAlreadyUsed =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_TOKEN_ALREADY_USED",
                    message: "Password reset token has already been used.");

            public static readonly Error TokenRevoked =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_TOKEN_REVOKED",
                    message: "Password reset token has been revoked.");

            public static readonly Error TokenExpired =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_TOKEN_EXPIRED",
                    message: "Password reset token has expired.");

            public static readonly Error TokenHashRequired =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_TOKEN_HASH_REQUIRED",
                    message: "Password reset token hash is required.");

            public static readonly Error TokenHashInvalid =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_TOKEN_HASH_INVALID",
                    message: "Password reset token hash is invalid.");

            public static readonly Error InvalidResetTokenId =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_INVALID_TOKEN_ID",
                    message: "Reset token id must be greater than zero.");

            public static readonly Error InvalidUserId =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_INVALID_USER_ID",
                    message: "User id must be greater than zero.");

            public static readonly Error InvalidExpiresAt =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_INVALID_EXPIRES_AT",
                    message: "Expiration time must be greater than created time.");

            public static readonly Error InvalidUsedAt =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_INVALID_USED_AT",
                    message: "Used time cannot be earlier than created time.");

            public static readonly Error InvalidRevokedAt =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_INVALID_REVOKED_AT",
                    message: "Revoked time cannot be earlier than created time.");

            public static readonly Error CreatedIpTooLong =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_CREATED_IP_TOO_LONG",
                    message: "Created IP must not exceed 45 characters.");

            public static readonly Error CorrelationIdTooLong =
                Error.Validation(
                    code: "IDENTITY.PASSWORD_RESET_CORRELATION_ID_TOO_LONG",
                    message: "Correlation id must not exceed 100 characters.");
        }

        public static class Refresh
        {
            public static readonly Error TokenNotFound =
                Error.NotFound(
                    code: "IDENTITY.REFRESH_TOKEN_NOT_FOUND",
                    message: "Refresh token was not found.");

            public static readonly Error TokenRevoked =
                Error.Validation(
                    code: "IDENTITY.REFRESH_TOKEN_REVOKED",
                    message: "Refresh token has been revoked.");

            public static readonly Error TokenExpired =
                Error.Validation(
                    code: "IDENTITY.REFRESH_TOKEN_EXPIRED",
                    message: "Refresh token has expired.");

            public static readonly Error TokenReplaced =
                Error.Validation(
                    code: "IDENTITY.REFRESH_TOKEN_REPLACED",
                    message: "Refresh token has already been replaced.");

            public static readonly Error TokenHashRequired =
                Error.Validation(
                    code: "IDENTITY.REFRESH_TOKEN_HASH_REQUIRED",
                    message: "Refresh token hash is required.");

            public static readonly Error TokenHashInvalid =
                Error.Validation(
                    code: "IDENTITY.REFRESH_TOKEN_HASH_INVALID",
                    message: "Refresh token hash is invalid.");

            public static readonly Error InvalidRefreshTokenId =
                Error.Validation(
                    code: "IDENTITY.REFRESH_INVALID_TOKEN_ID",
                    message: "Refresh token id must be greater than zero.");

            public static readonly Error InvalidUserId =
                Error.Validation(
                    code: "IDENTITY.REFRESH_INVALID_USER_ID",
                    message: "User id must be greater than zero.");

            public static readonly Error InvalidExpiresAt =
                Error.Validation(
                    code: "IDENTITY.REFRESH_INVALID_EXPIRES_AT",
                    message: "Expiration time must be greater than created time.");

            public static readonly Error InvalidRevokedAt =
                Error.Validation(
                    code: "IDENTITY.REFRESH_INVALID_REVOKED_AT",
                    message: "Revoked time cannot be earlier than created time.");

            public static readonly Error ReplacedByTokenHashInvalid =
                Error.Validation(
                    code: "IDENTITY.REFRESH_REPLACED_BY_TOKEN_HASH_INVALID",
                    message: "Replacement token hash is invalid.");

            public static readonly Error RevokedReasonTooLong =
                Error.Validation(
                    code: "IDENTITY.REFRESH_REVOKED_REASON_TOO_LONG",
                    message: "Revoked reason must not exceed 200 characters.");

            public static readonly Error CreatedIpTooLong =
                Error.Validation(
                    code: "IDENTITY.REFRESH_CREATED_IP_TOO_LONG",
                    message: "Created IP must not exceed 45 characters.");

            public static readonly Error UserAgentTooLong =
                Error.Validation(
                    code: "IDENTITY.REFRESH_USER_AGENT_TOO_LONG",
                    message: "User agent must not exceed 300 characters.");

            public static readonly Error CorrelationIdTooLong =
                Error.Validation(
                    code: "IDENTITY.REFRESH_CORRELATION_ID_TOO_LONG",
                    message: "Correlation id must not exceed 100 characters.");

            public static readonly Error RotationConflict =
                Error.Conflict(
                    code: "IDENTITY.REFRESH_ROTATION_CONFLICT",
                    message: "Refresh token rotation could not be completed deterministically.");
        }

        public static class Auth
        {
            public static readonly Error VerificationRequired =
                Error.Forbidden(
                    code: "IDENTITY.EMAIL_VERIFICATION_REQUIRED",
                    message: "Email verification is required for this operation.");

            public static readonly Error AccountInactive =
                Error.Forbidden(
                    code: "IDENTITY.ACCOUNT_INACTIVE",
                    message: "The account is inactive.");

            public static readonly Error LogoutFailed =
                Error.Failure(
                    code: "IDENTITY.LOGOUT_FAILED",
                    message: "Logout could not be completed.");
        }
    }
}