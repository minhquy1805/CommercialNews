using Audit.Domain.Entities;

namespace Audit.Application.Services.Evidence;

public interface IAuditEvidenceBuilder
{
    AuditLog Build(
        AuditEvidenceBuildInput input);
}