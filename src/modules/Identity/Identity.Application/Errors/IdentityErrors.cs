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
        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "IDENTITY.USER_INVALID_REQUEST",
                message: "The user request is invalid.");

        public static readonly Error NotFound =
            Error.NotFound(
                code: "IDENTITY.USER_NOT_FOUND",
                message: "User account was not found.");

        public static readonly Error QueryFailed =
            Error.Failure(
                code: "IDENTITY.USER_QUERY_FAILED",
                message: "User account query could not be completed.");

        public static readonly Error InvalidPaging =
            Error.Validation(
                code: "IDENTITY.USER_INVALID_PAGING",
                message: "User account paging parameters are invalid.");

        public static readonly Error InvalidDateRange =
            Error.Validation(
                code: "IDENTITY.USER_INVALID_DATE_RANGE",
                message: "User account date range is invalid.");

        public static readonly Error QueryTooLong =
            Error.Validation(
                code: "IDENTITY.USER_QUERY_TOO_LONG",
                message: "User account search query must not exceed 320 characters.");

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

        public static readonly Error ActivateFailed =
            Error.Failure(
                code: "IDENTITY.USER_ACTIVATE_FAILED",
                message: "User activation could not be completed.");

        public static readonly Error DisableFailed =
            Error.Failure(
                code: "IDENTITY.USER_DISABLE_FAILED",
                message: "User disabling could not be completed.");

        public static readonly Error LockFailed =
            Error.Failure(
                code: "IDENTITY.USER_LOCK_FAILED",
                message: "User lock could not be completed.");

        public static readonly Error UnlockFailed =
            Error.Failure(
                code: "IDENTITY.USER_UNLOCK_FAILED",
                message: "User unlock could not be completed.");

        public static readonly Error MarkEmailVerifiedFailed =
            Error.Failure(
                code: "IDENTITY.USER_MARK_EMAIL_VERIFIED_FAILED",
                message: "Marking user email as verified could not be completed.");

        public static readonly Error ProtectedAccount =
            Error.Forbidden(
                code: "IDENTITY.USER_PROTECTED_ACCOUNT",
                message: "This protected user account cannot be modified by this operation.");

        public static readonly Error SelfActionDenied =
            Error.Forbidden(
                code: "IDENTITY.USER_SELF_ACTION_DENIED",
                message: "This user operation cannot be performed on the current authenticated user.");

        public static readonly Error InvalidLockUntil =
            Error.Validation(
                code: "IDENTITY.USER_INVALID_LOCK_UNTIL",
                message: "LockedUntil must be a valid future timestamp.");

        public static readonly Error ChangePasswordFailed =
            Error.Failure(
                code: "IDENTITY.USER_CHANGE_PASSWORD_FAILED",
                message: "Password change could not be completed.");
    }

    public static class Auth
    {
        public static readonly Error Unauthenticated =
            Error.Unauthorized(
                code: "IDENTITY.UNAUTHENTICATED",
                message: "Authentication is required.");

        public static readonly Error Forbidden =
            Error.Forbidden(
                code: "IDENTITY.FORBIDDEN",
                message: "The authenticated caller is not allowed to perform this operation.");

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
        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "IDENTITY.PASSWORD_RESET_INVALID_REQUEST",
                message: "The password reset request is invalid.");

        public static readonly Error RequestFailed =
            Error.Failure(
                code: "IDENTITY.PASSWORD_RESET_REQUEST_FAILED",
                message: "The password reset request could not be completed.");

        public static readonly Error ResetFailed =
            Error.Failure(
                code: "IDENTITY.PASSWORD_RESET_FAILED",
                message: "Password reset could not be completed.");

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

    public static class Register
    {
        public static readonly Error RequestFailed =
            Error.Failure(
                code: "IDENTITY.REGISTER_FAILED",
                message: "Registration could not be completed.");
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

    public static class Session
    {
        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "IDENTITY.SESSION_INVALID_REQUEST",
                message: "The user session request is invalid.");

        public static readonly Error QueryFailed =
            Error.Failure(
                code: "IDENTITY.SESSION_QUERY_FAILED",
                message: "User session query could not be completed.");

        public static readonly Error RevokeFailed =
            Error.Failure(
                code: "IDENTITY.SESSION_REVOKE_FAILED",
                message: "User sessions could not be revoked.");
    }

    public static class LoginHistory
    {
        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "IDENTITY.LOGIN_HISTORY_INVALID_REQUEST",
                message: "The login history request is invalid.");

        public static readonly Error InvalidPaging =
            Error.Validation(
                code: "IDENTITY.LOGIN_HISTORY_INVALID_PAGING",
                message: "Login history paging parameters are invalid.");

        public static readonly Error InvalidDateRange =
            Error.Validation(
                code: "IDENTITY.LOGIN_HISTORY_INVALID_DATE_RANGE",
                message: "Login history date range is invalid.");

        public static readonly Error QueryFailed =
            Error.Failure(
                code: "IDENTITY.LOGIN_HISTORY_QUERY_FAILED",
                message: "Login history query could not be completed.");
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

        public static readonly Error AvatarFileRequired =
            Error.Validation(
                code: "IDENTITY.PROFILE_AVATAR_FILE_REQUIRED",
                message: "Avatar file is required.");

        public static readonly Error AvatarFileTooLarge =
            Error.Validation(
                code: "IDENTITY.PROFILE_AVATAR_FILE_TOO_LARGE",
                message: "Avatar file must not exceed 5 MB.");

        public static readonly Error AvatarFileTypeNotAllowed =
            Error.Validation(
                code: "IDENTITY.PROFILE_AVATAR_FILE_TYPE_NOT_ALLOWED",
                message: "Avatar file type is not allowed.");

        public static readonly Error AvatarUploadFailed =
            Error.Failure(
                code: "IDENTITY.PROFILE_AVATAR_UPLOAD_FAILED",
                message: "Avatar upload could not be completed.");

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
