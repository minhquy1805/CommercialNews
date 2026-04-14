namespace Audit.Application.Contracts.Requests;

public sealed class GetAuditLogByEventIdRequest
{
    public string AuditEventId { get; init; } = string.Empty;
}