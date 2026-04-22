namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.Outbox.Requests;

public sealed class MarkOutboxDeadHttpRequest
{
    public string? LastError { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorClass { get; init; }
}