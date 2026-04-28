namespace CommercialNews.BuildingBlocks.Outbox.Runtime.Models;

public sealed class DispatchOutboxMessageResult
{
    public bool Succeeded { get; init; }

    public bool IsRetryable { get; init; }

    public bool IsAmbiguous { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorClass { get; init; }

    public static DispatchOutboxMessageResult Success()
    {
        return new DispatchOutboxMessageResult
        {
            Succeeded = true,
            IsRetryable = false,
            IsAmbiguous = false
        };
    }

    public static DispatchOutboxMessageResult Failed(
        string? errorCode,
        string? errorMessage,
        string? errorClass,
        bool isRetryable,
        bool isAmbiguous = false)
    {
        return new DispatchOutboxMessageResult
        {
            Succeeded = false,
            IsRetryable = isRetryable,
            IsAmbiguous = isAmbiguous,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ErrorClass = errorClass
        };
    }
}