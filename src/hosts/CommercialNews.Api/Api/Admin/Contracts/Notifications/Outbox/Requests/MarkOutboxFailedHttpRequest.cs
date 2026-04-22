namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.Outbox.Requests;

public sealed class MarkOutboxFailedHttpRequest
{
    public DateTime? NextRetryAt { get; init; }

    public string? LastError { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorClass { get; init; }
}