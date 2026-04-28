namespace Notifications.Application.Contracts.Services;

public sealed class ProviderClassificationResult
{
    public bool IsSuccess { get; init; }

    public bool IsAmbiguous { get; init; }

    public string? ErrorClass { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ProviderMessageId { get; init; }

    public static ProviderClassificationResult Success(string? providerMessageId)
    {
        return new ProviderClassificationResult
        {
            IsSuccess = true,
            IsAmbiguous = false,
            ProviderMessageId = providerMessageId
        };
    }

    public static ProviderClassificationResult Failure(
        string errorClass,
        string? errorCode,
        string? errorMessage,
        bool isAmbiguous = false,
        string? providerMessageId = null)
    {
        return new ProviderClassificationResult
        {
            IsSuccess = false,
            IsAmbiguous = isAmbiguous,
            ErrorClass = errorClass,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ProviderMessageId = providerMessageId
        };
    }
}