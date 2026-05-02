using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Services;

public interface IAuditIngestionService
{
    Task<Result<AuditIngestionResult>> IngestAsync(
        AuditIngestionRequest request,
        CancellationToken cancellationToken = default);
}