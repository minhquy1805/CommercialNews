namespace Authorization.Application.Contracts.Outbox.Payload;

public sealed class PermissionUpdatedOutboxPayload
{
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public string PermissionKeyNormalized { get; init; } = string.Empty;
    public string? Module { get; init; }
    public string? Action { get; init; }
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public long? ActorUserId { get; init; }
    public string? CorrelationId { get; init; }
}