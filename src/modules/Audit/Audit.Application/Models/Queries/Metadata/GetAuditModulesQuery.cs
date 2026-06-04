using Audit.Application.Models.Results.Metadata;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.Metadata;

public sealed record GetAuditModulesQuery()
    : IRequest<Result<IReadOnlyList<AuditModuleResult>>>;
