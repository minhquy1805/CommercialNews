using Audit.Application.Models.Commands.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using Audit.Application.Services.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.Ingestion.IngestAuditEvent;

public sealed class IngestAuditEventCommandHandler
    : IRequestHandler<IngestAuditEventCommand, Result<IngestAuditEventResult>>
{
    private readonly IAuditIngestionApplicationService _auditIngestionApplicationService;

    public IngestAuditEventCommandHandler(
        IAuditIngestionApplicationService auditIngestionApplicationService)
    {
        _auditIngestionApplicationService = auditIngestionApplicationService
            ?? throw new ArgumentNullException(nameof(auditIngestionApplicationService));
    }

    public async Task<Result<IngestAuditEventResult>> Handle(
        IngestAuditEventCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _auditIngestionApplicationService.IngestAsync(
            request,
            cancellationToken);
    }
}