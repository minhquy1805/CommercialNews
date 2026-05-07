using Identity.Domain.Exceptions;

namespace Identity.Domain.Entities;

public sealed class LoginHistory
{
    private const int EmailNormalizedAttemptedMaxLength = 320;
    private const int FailureReasonMaxLength = 100;
    private const int IpAddressMaxLength = 45;
    private const int UserAgentMaxLength = 300;
    private const int CorrelationIdMaxLength = 100;

    private LoginHistory(
        long loginId,
        long? userId,
        string? emailNormalizedAttempted,
        bool succeeded,
        string? failureReason,
        DateTime attemptedAt,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        LoginId = loginId;
        UserId = userId;
        EmailNormalizedAttempted = emailNormalizedAttempted;
        Succeeded = succeeded;
        FailureReason = failureReason;
        AttemptedAt = attemptedAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
    }

    public long LoginId { get; private set; }
    public long? UserId { get; private set; }
    public string? EmailNormalizedAttempted { get; private set; }
    public bool Succeeded { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime AttemptedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }

    public static LoginHistory CreateSuccess(
        long userId,
        string? emailNormalizedAttempted,
        DateTime attemptedAt,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        if (userId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.LOGIN_HISTORY_INVALID_USER_ID",
                "User id must be greater than zero.");
        }

        ValidateCommon(
            emailNormalizedAttempted,
            failureReason: null,
            attemptedAt,
            ipAddress,
            userAgent,
            correlationId);

        return new LoginHistory(
            loginId: 0,
            userId: userId,
            emailNormalizedAttempted: NormalizeOptional(emailNormalizedAttempted),
            succeeded: true,
            failureReason: null,
            attemptedAt: attemptedAt,
            ipAddress: NormalizeOptional(ipAddress),
            userAgent: NormalizeOptional(userAgent),
            correlationId: NormalizeOptional(correlationId));
    }

    public static LoginHistory CreateFailure(
        long? userId,
        string? emailNormalizedAttempted,
        string? failureReason,
        DateTime attemptedAt,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        if (userId.HasValue && userId.Value <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.LOGIN_HISTORY_INVALID_USER_ID",
                "User id must be greater than zero.");
        }

        ValidateCommon(
            emailNormalizedAttempted,
            failureReason,
            attemptedAt,
            ipAddress,
            userAgent,
            correlationId);

        return new LoginHistory(
            loginId: 0,
            userId: userId,
            emailNormalizedAttempted: NormalizeOptional(emailNormalizedAttempted),
            succeeded: false,
            failureReason: NormalizeOptional(failureReason),
            attemptedAt: attemptedAt,
            ipAddress: NormalizeOptional(ipAddress),
            userAgent: NormalizeOptional(userAgent),
            correlationId: NormalizeOptional(correlationId));
    }

    public static LoginHistory Rehydrate(
        long loginId,
        long? userId,
        string? emailNormalizedAttempted,
        bool succeeded,
        string? failureReason,
        DateTime attemptedAt,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        if (loginId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.LOGIN_HISTORY_INVALID_LOGIN_ID",
                "Login id must be greater than zero.");
        }

        if (userId.HasValue && userId.Value <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.LOGIN_HISTORY_INVALID_USER_ID",
                "User id must be greater than zero.");
        }

        ValidateCommon(
            emailNormalizedAttempted,
            failureReason,
            attemptedAt,
            ipAddress,
            userAgent,
            correlationId);

        if (succeeded && !string.IsNullOrWhiteSpace(failureReason))
        {
            throw new IdentityDomainException(
                "IDENTITY.LOGIN_HISTORY_SUCCESS_WITH_FAILURE_REASON",
                "Successful login history cannot have a failure reason.");
        }

        return new LoginHistory(
            loginId: loginId,
            userId: userId,
            emailNormalizedAttempted: NormalizeOptional(emailNormalizedAttempted),
            succeeded: succeeded,
            failureReason: succeeded ? null : NormalizeOptional(failureReason),
            attemptedAt: attemptedAt,
            ipAddress: NormalizeOptional(ipAddress),
            userAgent: NormalizeOptional(userAgent),
            correlationId: NormalizeOptional(correlationId));
    }

    private static void ValidateCommon(
        string? emailNormalizedAttempted,
        string? failureReason,
        DateTime attemptedAt,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        EnsureValidTimestamp(
            attemptedAt,
            "IDENTITY.LOGIN_HISTORY_INVALID_ATTEMPTED_AT");

        ValidateLength(
            emailNormalizedAttempted,
            EmailNormalizedAttemptedMaxLength,
            "IDENTITY.LOGIN_HISTORY_EMAIL_NORMALIZED_ATTEMPTED_TOO_LONG",
            "EmailNormalizedAttempted");

        ValidateLength(
            failureReason,
            FailureReasonMaxLength,
            "IDENTITY.LOGIN_HISTORY_FAILURE_REASON_TOO_LONG",
            "FailureReason");

        ValidateLength(
            ipAddress,
            IpAddressMaxLength,
            "IDENTITY.LOGIN_HISTORY_IP_ADDRESS_TOO_LONG",
            "IpAddress");

        ValidateLength(
            userAgent,
            UserAgentMaxLength,
            "IDENTITY.LOGIN_HISTORY_USER_AGENT_TOO_LONG",
            "UserAgent");

        ValidateLength(
            correlationId,
            CorrelationIdMaxLength,
            "IDENTITY.LOGIN_HISTORY_CORRELATION_ID_TOO_LONG",
            "CorrelationId");
    }

    private static void ValidateLength(
        string? value,
        int maxLength,
        string code,
        string fieldName)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            throw new IdentityDomainException(
                code,
                $"{fieldName} must not exceed {maxLength} characters.");
        }
    }

    private static void EnsureValidTimestamp(DateTime value, string code)
    {
        if (value == default)
        {
            throw new IdentityDomainException(code, "Timestamp is required.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}