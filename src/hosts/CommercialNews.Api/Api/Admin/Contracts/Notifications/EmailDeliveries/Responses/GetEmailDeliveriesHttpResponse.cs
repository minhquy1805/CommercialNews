namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Responses;

public sealed class GetEmailDeliveriesHttpResponse
{
    public IReadOnlyCollection<EmailDeliveryListItemHttpResponse> Items { get; init; }
        = Array.Empty<EmailDeliveryListItemHttpResponse>();

    public NotificationPageInfoHttpResponse PageInfo { get; init; } = new();
}

public sealed class EmailDeliveryListItemHttpResponse
{
    public long EmailDeliveryId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public long? RecipientUserId { get; init; }

    public string? ToEmail { get; init; }

    public string? ToEmailHash { get; init; }

    public string TemplateKey { get; init; } = string.Empty;

    public int? TemplateVersion { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTime? LastAttemptAt { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public DateTime? SentAt { get; init; }

    public DateTime? FailedAt { get; init; }

    public DateTime? DeadAt { get; init; }

    public DateTime? SuppressedAt { get; init; }

    public DateTime? AmbiguousAt { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorClass { get; init; }

    public string? CorrelationId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public sealed class NotificationPageInfoHttpResponse
{
    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}