namespace Notifications.Application.Models.QueryModels;

public sealed class EmailDeliveryListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public DateTime? FromCreatedAt { get; init; }

    public DateTime? ToCreatedAt { get; init; }

    public long? RecipientUserId { get; init; }

    public string? TemplateKey { get; init; }

    public string? Status { get; init; }

    public string? CorrelationId { get; init; }

    public string? MessageId { get; init; }
}