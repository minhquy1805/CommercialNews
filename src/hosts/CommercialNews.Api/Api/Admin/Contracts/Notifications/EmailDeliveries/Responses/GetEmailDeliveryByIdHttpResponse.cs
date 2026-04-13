namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Responses;

public sealed class GetEmailDeliveryByIdHttpResponse
{
    public long EmailDeliveryId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string BusinessDedupeKey { get; init; } = string.Empty;

    public long? RecipientUserId { get; init; }

    public string ToEmail { get; init; } = string.Empty;

    public string? ToEmailHash { get; init; }

    public string TemplateKey { get; init; } = string.Empty;

    public int? TemplateVersion { get; init; }

    public string? Subject { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? ProviderMessageId { get; init; }

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTime? LastAttemptAt { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public DateTime? SentAt { get; init; }

    public DateTime? FailedAt { get; init; }

    public DateTime? DeadAt { get; init; }

    public DateTime? SuppressedAt { get; init; }

    public DateTime? AmbiguousAt { get; init; }

    public string? LastError { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorClass { get; init; }

    public string? CorrelationId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public IReadOnlyCollection<EmailDeliveryAttemptHttpResponse> Attempts { get; init; }
        = Array.Empty<EmailDeliveryAttemptHttpResponse>();
}

public sealed class EmailDeliveryAttemptHttpResponse
{
    public long EmailDeliveryAttemptId { get; init; }

    public long EmailDeliveryId { get; init; }

    public int AttemptNumber { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public bool IsAmbiguous { get; init; }

    public string? ProviderMessageId { get; init; }

    public string? ProviderErrorCode { get; init; }

    public string? ErrorClass { get; init; }

    public string? ErrorDetail { get; init; }

    public string? CorrelationId { get; init; }

    public DateTime CreatedAt { get; init; }
}