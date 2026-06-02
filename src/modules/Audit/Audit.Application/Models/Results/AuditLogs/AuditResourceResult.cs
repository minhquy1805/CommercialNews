namespace Audit.Application.Models.Results.AuditLogs;

public sealed record AuditResourceResult(
    string ResourceType,
    string ResourceId,
    string? ResourceDisplayName);