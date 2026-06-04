using Audit.Application.Models.Queries.Metadata;
using Audit.Application.Models.Results.Metadata;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.Metadata.GetAuditModules;

public sealed class GetAuditModulesQueryHandler
    : IRequestHandler<GetAuditModulesQuery, Result<IReadOnlyList<AuditModuleResult>>>
{
    public Task<Result<IReadOnlyList<AuditModuleResult>>> Handle(
        GetAuditModulesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(
            Result<IReadOnlyList<AuditModuleResult>>.Success(
                AuditMetadataCatalog.CurrentModules));
    }
}
