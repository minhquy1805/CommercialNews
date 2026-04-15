using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.UseCases.GetAuditLogsByCorrelationId;

public interface IGetAuditLogsByCorrelationIdUseCase
{
    Task<Result<GetAuditLogsByCorrelationIdResponse>> ExecuteAsync(
        GetAuditLogsByCorrelationIdRequest request,
        CancellationToken cancellationToken = default);
}