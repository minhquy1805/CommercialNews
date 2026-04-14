namespace Audit.Application.Contracts.Requests;

public sealed class GetAuditLogsByCorrelationIdRequest
{
    public string CorrelationId { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}