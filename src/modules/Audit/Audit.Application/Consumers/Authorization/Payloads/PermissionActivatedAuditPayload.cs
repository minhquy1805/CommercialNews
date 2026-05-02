namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record PermissionActivatedAuditPayload(
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    string? PermissionModule,
    string? PermissionAction,
    bool PermissionIsSystem,
    long? ActivatedByUserId,
    DateTime ActivatedAtUtc,
    string BusinessDedupeKey);