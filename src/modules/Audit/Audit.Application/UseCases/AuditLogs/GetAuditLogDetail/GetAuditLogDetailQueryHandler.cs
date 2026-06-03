using Audit.Application.Abstractions.Persistence;
using Audit.Application.Errors;
using Audit.Application.Models.Queries.AuditLogs;
using Audit.Application.Models.Results.AuditLogs;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.AuditLogs.GetAuditLogDetail;

public sealed class GetAuditLogDetailQueryHandler
    : IRequestHandler<GetAuditLogDetailQuery, Result<AuditLogDetailResult>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetAuditLogDetailQueryHandler(
        IAuditLogRepository auditLogRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<AuditLogDetailResult>> Handle(
        GetAuditLogDetailQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLog = await _auditLogRepository.GetByPublicIdAsync(
            request.PublicId,
            cancellationToken);

        if (auditLog is null)
        {
            return Result<AuditLogDetailResult>.Failure(
                AuditErrors.AuditLog.NotFound);
        }

        var result = _auditResultMapper.ToAuditLogDetail(
            auditLog);

        return Result<AuditLogDetailResult>.Success(
            result);
    }
}