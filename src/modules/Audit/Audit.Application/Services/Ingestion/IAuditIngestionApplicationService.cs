using Audit.Application.Models.Commands.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Services.Ingestion;

public interface IAuditIngestionApplicationService
{
    Task<Result<IngestAuditEventResult>> IngestAsync(
        IngestAuditEventCommand command,
        CancellationToken cancellationToken = default);
}