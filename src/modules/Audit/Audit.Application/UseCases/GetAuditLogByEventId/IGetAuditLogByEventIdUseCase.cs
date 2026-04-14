using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Audit.Application.UseCases.GetAuditLogByEventId;

public interface IGetAuditLogByEventIdUseCase
{
    Task<Result<GetAuditLogByEventIdResponse>> ExecuteAsync(
        GetAuditLogByEventIdRequest request,
        CancellationToken cancellationToken = default);
}