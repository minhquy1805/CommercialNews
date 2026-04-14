namespace Audit.Application.Contracts.Requests;

public sealed class GetAuditLogByIdRequest
{
    public long AuditId { get; init; }
}