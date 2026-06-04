using Audit.Application.Models.Results.AuditLogs;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.AuditLogs;

public sealed record GetAuditLogDetailQuery(
    string PublicId)
    : IRequest<Result<AuditLogDetailResult>>;