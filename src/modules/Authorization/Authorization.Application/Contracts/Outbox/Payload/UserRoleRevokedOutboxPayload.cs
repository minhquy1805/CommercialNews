namespace Authorization.Application.Contracts.Outbox.Payload;

public sealed record UserRoleRevokedOutboxPayload(
    long UserId,
    long RoleId,
    string RolePublicId,
    string RoleName,
    long? ActorUserId,
    string? CorrelationId,
    DateTime OccurredAtUtc);