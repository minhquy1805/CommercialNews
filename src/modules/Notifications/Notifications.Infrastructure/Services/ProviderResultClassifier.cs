using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Application.Services;

public sealed class ProviderResultClassifier : IProviderResultClassifier
{
    public ProviderClassificationResult Classify(EmailSendResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return ProviderClassificationResult.Success(
                result.ProviderMessageId);
        }

        if (result.IsAmbiguous)
        {
            return ProviderClassificationResult.Failure(
                errorClass: EmailErrorClass.Ambiguous,
                errorCode: NormalizeErrorCode(result.ProviderErrorCode, "PROVIDER_AMBIGUOUS"),
                errorMessage: result.ProviderErrorMessage,
                isAmbiguous: true,
                providerMessageId: result.ProviderMessageId);
        }

        string errorCode = NormalizeErrorCode(
            result.ProviderErrorCode,
            "PROVIDER_UNKNOWN");

        string errorClass = ClassifyErrorClass(errorCode);

        return ProviderClassificationResult.Failure(
            errorClass: errorClass,
            errorCode: errorCode,
            errorMessage: result.ProviderErrorMessage,
            isAmbiguous: false,
            providerMessageId: result.ProviderMessageId);
    }

    private static string ClassifyErrorClass(string errorCode)
    {
        if (errorCode.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            return EmailErrorClass.Ambiguous;
        }

        if (errorCode.Contains("AUTH", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("TEMPLATE", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("VALIDATION", StringComparison.OrdinalIgnoreCase))
        {
            return EmailErrorClass.Permanent;
        }

        if (errorCode.Contains("SMTP_COMMAND", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("SMTP_PROTOCOL", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("SMTP_UNKNOWN", StringComparison.OrdinalIgnoreCase))
        {
            return EmailErrorClass.Provider;
        }

        if (errorCode.Contains("TRANSIENT", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("RATE_LIMIT", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("TEMP", StringComparison.OrdinalIgnoreCase))
        {
            return EmailErrorClass.Transient;
        }

        return EmailErrorClass.Unknown;
    }

    private static string NormalizeErrorCode(
        string? errorCode,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return fallback;
        }

        return errorCode.Trim();
    }
}