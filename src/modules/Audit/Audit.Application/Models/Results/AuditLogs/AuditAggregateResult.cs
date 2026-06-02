namespace Audit.Application.Models.Results.AuditLogs;

public sealed record AuditAggregateResult(
    string? AggregateType,
    string? AggregateId,
    string? AggregatePublicId,
    int? AggregateVersion);