using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Audit.Application.UseCases.GetAuditLogs;

public interface IGetAuditLogsUseCase
{
    Task<Result<GetAuditLogsResponse>> ExecuteAsync(
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default);
}