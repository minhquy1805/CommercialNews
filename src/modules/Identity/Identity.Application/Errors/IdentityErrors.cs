using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Identity.Application.Errors;

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

    public static readonly Error PasswordPolicyViolation =
        Error.Validation(
            code: "IDENTITY.PASSWORD_POLICY_VIOLATION",
            message: "The password does not satisfy the password policy.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict(
            code: "IDENTITY.EMAIL_EXISTS",
            message: "An account with this email already exists.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "IDENTITY.RATE_LIMITED",
            message: "Too many identity requests. Please try again later.");

    public static class User
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "IDENTITY.USER_NOT_FOUND",
                message: "User account was not found.");

        public static readonly Error InvalidStatus =
            Error.Validation(
                code: "IDENTITY.USER_INVALID_STATUS",
                message: "User status is invalid.");

        public static readonly Error AlreadyVerified =
            Error.Validation(
                code: "IDENTITY.USER_ALREADY_VERIFIED",
                message: "The user email is already verified.");

        public static readonly Error CannotActivateUnverified =
            Error.Validation(
                code: "IDENTITY.USER_CANNOT_ACTIVATE_UNVERIFIED",
                message: "An unverified user cannot be activated.");
    }

    public static class Auth
    {
        public static readonly Error VerificationRequired =
            Error.Forbidden(
                code: "IDENTITY.EMAIL_VERIFICATION_REQUIRED",
                message: "Email verification is required for this operation.");

        public static readonly Error AccountLocked =
            Error.Forbidden(
                code: "IDENTITY.ACCOUNT_LOCKED",
                message: "The account is locked.");

        public static readonly Error AccountDisabled =
            Error.Forbidden(
                code: "IDENTITY.ACCOUNT_DISABLED",
                message: "The account is disabled.");

        public static readonly Error LogoutFailed =
            Error.Failure(
                code: "IDENTITY.LOGOUT_FAILED",
                message: "Logout could not be completed.");
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
    }

    public static class Refresh
    {
        public static readonly Error TokenNotFound =
            Error.NotFound(
                code: "IDENTITY.REFRESH_TOKEN_NOT_FOUND",
                message: "Refresh token was not found.");

        public static readonly Error TokenRevoked =
            Error.Unauthorized(
                code: "IDENTITY.REFRESH_TOKEN_REVOKED",
                message: "Refresh token has been revoked.");

        public static readonly Error TokenExpired =
            Error.Unauthorized(
                code: "IDENTITY.REFRESH_TOKEN_EXPIRED",
                message: "Refresh token has expired.");

        public static readonly Error TokenReplaced =
            Error.Unauthorized(
                code: "IDENTITY.REFRESH_TOKEN_REPLACED",
                message: "Refresh token has already been replaced.");

        public static readonly Error ReuseDetected =
            Error.Unauthorized(
                code: "IDENTITY.REFRESH_REUSE_DETECTED",
                message: "Refresh token reuse was detected.");

        public static readonly Error RotationConflict =
            Error.Conflict(
                code: "IDENTITY.REFRESH_ROTATION_CONFLICT",
                message: "Refresh token rotation could not be completed deterministically.");

        public static readonly Error InvalidReplacementState =
            Error.Validation(
                code: "IDENTITY.REFRESH_INVALID_REPLACEMENT_STATE",
                message: "A replaced refresh token must also be revoked.");
    }

    public static class Profile
    {
        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "IDENTITY.PROFILE_INVALID_REQUEST",
                message: "The profile update request is invalid.");

        public static readonly Error FullNameTooLong =
            Error.Validation(
                code: "IDENTITY.PROFILE_FULL_NAME_TOO_LONG",
                message: "Full name must not exceed 200 characters.");

        public static readonly Error AvatarUrlTooLong =
            Error.Validation(
                code: "IDENTITY.PROFILE_AVATAR_URL_TOO_LONG",
                message: "Avatar URL must not exceed 800 characters.");

        public static readonly Error UpdateFailed =
            Error.Failure(
                code: "IDENTITY.PROFILE_UPDATE_FAILED",
                message: "Profile update could not be completed.");
    }

    public static class ResendVerification
    {
        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "IDENTITY.RESEND_VERIFICATION_INVALID_REQUEST",
                message: "The resend verification request is invalid.");

        public static readonly Error RequestFailed =
            Error.Failure(
                code: "IDENTITY.RESEND_VERIFICATION_FAILED",
                message: "The verification email request could not be completed.");
    }

    public static class Logout
    {
        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "IDENTITY.LOGOUT_INVALID_REQUEST",
                message: "The logout request is invalid.");

        public static readonly Error NotAuthenticated =
            Error.Unauthorized(
                code: "IDENTITY.LOGOUT_NOT_AUTHENTICATED",
                message: "Authentication is required for logout.");

        public static readonly Error Forbidden =
            Error.Forbidden(
                code: "IDENTITY.LOGOUT_FORBIDDEN",
                message: "The refresh token does not belong to the current user.");

        public static readonly Error Failed =
            Error.Failure(
                code: "IDENTITY.LOGOUT_FAILED",
                message: "Logout could not be completed.");
    }

    public static class LogoutAllSessions
    {
        public static readonly Error NotAuthenticated =
            Error.Unauthorized(
                code: "IDENTITY.LOGOUT_ALL_NOT_AUTHENTICATED",
                message: "Authentication is required to log out all sessions.");

        public static readonly Error Failed =
            Error.Failure(
                code: "IDENTITY.LOGOUT_ALL_FAILED",
                message: "Logging out all sessions could not be completed.");
    }
}