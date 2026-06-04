namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

public sealed class AuditActorHttpResponse
{
    public long? ActorInternalId { get; init; }

    public string? ActorUserId { get; init; }

    public string? ActorEmail { get; init; }

    public string? ActorDisplayName { get; init; }

    public string ActorType { get; init; } = string.Empty;
}
