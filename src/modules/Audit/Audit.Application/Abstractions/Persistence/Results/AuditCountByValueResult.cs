namespace Audit.Application.Abstractions.Persistence.Results;

public sealed record AuditCountByValueResult(
    string Value,
    int Count);