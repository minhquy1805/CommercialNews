using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Services;

public sealed class NotificationRetryPolicy : INotificationRetryPolicy
{
    private const int MaxTransientAttempts = 3;
    private const int MaxAmbiguousAttempts = 2;

    private const int TransientRetryDelayMinutes = 1;
    private const int AmbiguousRetryDelayMinutes = 2;

    public RetryDecision Evaluate(NotificationRetryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Important:
        // If the current delivery is already in a terminal success state,
        // we never retry from this policy. The caller should treat this as no-op
        // or reject the transition at the use-case level.
        if (string.Equals(context.CurrentStatus, EmailDeliveryStatus.Sent, StringComparison.OrdinalIgnoreCase))
        {
            return new RetryDecision
            {
                ShouldRetry = false,
                ShouldMarkDead = false,
                NextRetryAt = null,
                Reason = "The email delivery is already sent."
            };
        }

        // Important:
        // Template/validation failures are considered deterministic failures.
        // Retrying them usually repeats the same invalid input or broken template,
        // so we stop immediately and move to dead.
        if (IsTemplateOrValidationFailure(context.ErrorClass))
        {
            return new RetryDecision
            {
                ShouldRetry = false,
                ShouldMarkDead = true,
                NextRetryAt = null,
                Reason = "Template or validation failures should not be retried."
            };
        }

        // Important:
        // Policy failures are not transport failures.
        // They usually mean the system decided not to send,
        // so the caller can suppress instead of retrying or dead-lettering.
        if (string.Equals(context.ErrorClass, EmailErrorClass.Policy, StringComparison.OrdinalIgnoreCase))
        {
            return new RetryDecision
            {
                ShouldRetry = false,
                ShouldMarkDead = false,
                NextRetryAt = null,
                Reason = "Policy failures should not be retried."
            };
        }

        // Important:
        // Ambiguous means we do not know for sure whether the provider accepted the email.
        // We allow fewer retries here because repeated sends may increase duplicate-send risk.
        if (context.IsAmbiguous ||
            string.Equals(context.ErrorClass, EmailErrorClass.Ambiguous, StringComparison.OrdinalIgnoreCase))
        {
            if (context.AttemptCount >= MaxAmbiguousAttempts)
            {
                return new RetryDecision
                {
                    ShouldRetry = false,
                    ShouldMarkDead = true,
                    NextRetryAt = null,
                    Reason = "Ambiguous outcome exceeded the allowed retry threshold."
                };
            }

            return new RetryDecision
            {
                ShouldRetry = true,
                ShouldMarkDead = false,
                NextRetryAt = context.NowUtc.AddMinutes(AmbiguousRetryDelayMinutes),
                Reason = "Ambiguous provider outcome should be retried conservatively."
            };
        }

        // Important:
        // Transient and provider temporary failures are retry candidates.
        // These are the classic cases such as timeout, temporary unavailability,
        // or short-lived network/provider interruptions.
        if (IsRetryableTransportFailure(context.ErrorClass, context.ErrorCode))
        {
            if (context.AttemptCount >= MaxTransientAttempts)
            {
                return new RetryDecision
                {
                    ShouldRetry = false,
                    ShouldMarkDead = true,
                    NextRetryAt = null,
                    Reason = "Retryable transport failures exceeded the retry threshold."
                };
            }

            return new RetryDecision
            {
                ShouldRetry = true,
                ShouldMarkDead = false,
                NextRetryAt = context.NowUtc.AddMinutes(TransientRetryDelayMinutes),
                Reason = "Retryable transport failure should be retried."
            };
        }

        // Important:
        // Permanent/provider-rejected failures are treated as terminal.
        // The caller should move the delivery to dead because a retry is not expected
        // to change the result.
        if (IsPermanentFailure(context.ErrorClass, context.ErrorCode))
        {
            return new RetryDecision
            {
                ShouldRetry = false,
                ShouldMarkDead = true,
                NextRetryAt = null,
                Reason = "Permanent provider failure should not be retried."
            };
        }

        // Important:
        // Fallback rule:
        // if the failure type is unknown, fail safely by not retrying indefinitely.
        // We prefer terminating rather than causing unbounded retry loops.
        return new RetryDecision
        {
            ShouldRetry = false,
            ShouldMarkDead = true,
            NextRetryAt = null,
            Reason = "Unknown failure type was treated as terminal."
        };
    }

    private static bool IsTemplateOrValidationFailure(string? errorClass)
    {
        return string.Equals(errorClass, EmailErrorClass.Template, StringComparison.OrdinalIgnoreCase)
            || string.Equals(errorClass, EmailErrorClass.Validation, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRetryableTransportFailure(string? errorClass, string? errorCode)
    {
        if (string.Equals(errorClass, EmailErrorClass.Transient, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(errorCode, NotificationServiceErrorCodes.NetworkTimeout, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.ProviderTemporaryUnavailable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.Smtp421, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.Smtp451, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsPermanentFailure(string? errorClass, string? errorCode)
    {
        if (string.Equals(errorClass, EmailErrorClass.Permanent, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(errorClass, EmailErrorClass.Provider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(errorCode, NotificationServiceErrorCodes.ProviderRejected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(errorCode, NotificationServiceErrorCodes.Smtp550, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.Smtp553, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}