namespace Authorization.Application.Contracts.Outbox.Payload;

public sealed class RoleCreatedOutboxPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string RoleNameNormalized { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public long? ActorUserId { get; init; }
    public string? CorrelationId { get; init; }
}