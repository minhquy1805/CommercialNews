namespace Authorization.Application.Contracts.Outbox.Payload;

public sealed class PermissionDeactivatedOutboxPayload
{
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public long? ActorUserId { get; init; }
    public string? CorrelationId { get; init; }
}