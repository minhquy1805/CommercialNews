namespace Authorization.Application.Contracts.Outbox.Payload;

public sealed record RolePermissionGrantedOutboxPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    long? ActorUserId,
    string? CorrelationId,
    DateTime OccurredAtUtc);