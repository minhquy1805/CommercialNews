using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Audit.Application.UseCases.GetAuditLogById;

public interface IGetAuditLogByIdUseCase
{
    Task<Result<GetAuditLogByIdResponse>> ExecuteAsync(
        GetAuditLogByIdRequest request,
        CancellationToken cancellationToken = default);
}