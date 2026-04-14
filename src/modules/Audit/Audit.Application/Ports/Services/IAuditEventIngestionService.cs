using Audit.Application.Models;
using Audit.Domain.Entities;

namespace Audit.Application.Ports.Services;

public interface IAuditEventIngestionService
{
    Task<AuditIngestionResult> IngestAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);
}