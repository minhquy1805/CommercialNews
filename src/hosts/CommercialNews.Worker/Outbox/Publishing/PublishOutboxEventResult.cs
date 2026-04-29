namespace CommercialNews.Worker.Outbox.Publishing;

public sealed record PublishOutboxEventResult(
    bool Succeeded,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ErrorClass = null,
    bool IsRetryable = true,
    bool IsAmbiguous = false)
{
    public static PublishOutboxEventResult Success()
    {
        return new PublishOutboxEventResult(Succeeded: true);
    }

    public static PublishOutboxEventResult Failed(
        string? errorCode,
        string? errorMessage,
        string? errorClass,
        bool isRetryable,
        bool isAmbiguous = false)
    {
        return new PublishOutboxEventResult(
            Succeeded: false,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage,
            ErrorClass: errorClass,
            IsRetryable: isRetryable,
            IsAmbiguous: isAmbiguous);
    }
}