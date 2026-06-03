using Audit.Application.Abstractions.Persistence;
using Audit.Application.Errors;
using Audit.Application.Models.Queries.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.Ingestion.GetAuditIngestionDetail;

public sealed class GetAuditIngestionDetailQueryHandler
    : IRequestHandler<GetAuditIngestionDetailQuery, Result<AuditIngestionDetailResult>>
{
    private readonly IAuditIngestionRepository _auditIngestionRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetAuditIngestionDetailQueryHandler(
        IAuditIngestionRepository auditIngestionRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditIngestionRepository = auditIngestionRepository
            ?? throw new ArgumentNullException(nameof(auditIngestionRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<AuditIngestionDetailResult>> Handle(
        GetAuditIngestionDetailQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ingestion = await _auditIngestionRepository.GetByPublicIdAsync(
            request.PublicId,
            cancellationToken);

        if (ingestion is null)
        {
            return Result<AuditIngestionDetailResult>.Failure(
                AuditErrors.Ingestion.NotFound);
        }

        var result = _auditResultMapper.ToAuditIngestionDetail(
            ingestion);

        return Result<AuditIngestionDetailResult>.Success(
            result);
    }
}