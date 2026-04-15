using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.UseCases.GetAuditLogByEventId;

public interface IGetAuditLogByEventIdUseCase
{
    Task<Result<GetAuditLogByEventIdResponse>> ExecuteAsync(
        GetAuditLogByEventIdRequest request,
        CancellationToken cancellationToken = default);
}