using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.UseCases.GetAuditLogs;

public interface IGetAuditLogsUseCase
{
    Task<Result<GetAuditLogsResponse>> ExecuteAsync(
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default);
}