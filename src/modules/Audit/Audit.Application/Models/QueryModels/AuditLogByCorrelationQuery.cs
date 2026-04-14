namespace Audit.Application.Models.QueryModels;

public sealed class AuditLogByCorrelationQuery
{
    public string CorrelationId { get; init; } = string.Empty;

    public int Skip { get; init; }

    public int Take { get; init; } = 20;
}