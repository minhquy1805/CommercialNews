using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Services;

public sealed class ProviderResultClassifier : IProviderResultClassifier
{
    public ProviderClassificationResult Classify(EmailSendResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Important:
        // If the sender already confirmed success, the classifier should not
        // reinterpret it as a failure. This is the strongest signal we have.
        if (result.IsSuccess)
        {
            return new ProviderClassificationResult
            {
                IsSuccess = true,
                IsAmbiguous = false,
                IsRetryable = false,
                ErrorClass = null,
                ErrorCode = null,
                ErrorMessage = null
            };
        }

        // Important:
        // Ambiguous outcomes are special because we do not know whether the provider
        // may have accepted the email before the client lost certainty.
        // We keep them separate from normal transient failures.
        if (result.IsAmbiguous ||
            string.Equals(result.ProviderErrorCode, NotificationServiceErrorCodes.AmbiguousTimeout, StringComparison.OrdinalIgnoreCase))
        {
            return new ProviderClassificationResult
            {
                IsSuccess = false,
                IsAmbiguous = true,
                IsRetryable = true,
                ErrorClass = EmailErrorClass.Ambiguous,
                ErrorCode = result.ProviderErrorCode ?? NotificationServiceErrorCodes.AmbiguousTimeout,
                ErrorMessage = result.ProviderErrorMessage ?? "The provider outcome is ambiguous."
            };
        }

        string? errorCode = Normalize(result.ProviderErrorCode);
        string? errorMessage = Normalize(result.ProviderErrorMessage);

        // Important:
        // Network/transport timeouts are classic retryable failures.
        if (string.Equals(errorCode, NotificationServiceErrorCodes.NetworkTimeout, StringComparison.OrdinalIgnoreCase))
        {
            return new ProviderClassificationResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                IsRetryable = true,
                ErrorClass = EmailErrorClass.Transient,
                ErrorCode = NotificationServiceErrorCodes.NetworkTimeout,
                ErrorMessage = errorMessage ?? "The email send operation timed out."
            };
        }

        // Important:
        // Temporary provider availability failures are retryable.
        if (string.Equals(errorCode, NotificationServiceErrorCodes.ProviderTemporaryUnavailable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.Smtp421, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.Smtp451, StringComparison.OrdinalIgnoreCase))
        {
            return new ProviderClassificationResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                IsRetryable = true,
                ErrorClass = EmailErrorClass.Transient,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage ?? "The email provider is temporarily unavailable."
            };
        }

        // Important:
        // Explicit provider rejection is treated as non-retryable/permanent.
        if (string.Equals(errorCode, NotificationServiceErrorCodes.ProviderRejected, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.Smtp550, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, NotificationServiceErrorCodes.Smtp553, StringComparison.OrdinalIgnoreCase))
        {
            return new ProviderClassificationResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                IsRetryable = false,
                ErrorClass = EmailErrorClass.Permanent,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage ?? "The provider rejected the email delivery request."
            };
        }

        // Important:
        // Fallback for unknown provider/send failures.
        // We classify them as provider failures and do not retry automatically
        // unless you later choose to widen this policy.
        return new ProviderClassificationResult
        {
            IsSuccess = false,
            IsAmbiguous = false,
            IsRetryable = false,
            ErrorClass = EmailErrorClass.Provider,
            ErrorCode = errorCode ?? NotificationServiceErrorCodes.ProviderRejected,
            ErrorMessage = errorMessage ?? "An unknown provider failure occurred."
        };
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}