namespace Authorization.Application.Contracts.Outbox.Payload;

public sealed class RoleActivatedOutboxPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public long? ActorUserId { get; init; }
    public string? CorrelationId { get; init; }
}