using Audit.Application.Models.Results.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.Ingestion;

public sealed record GetAuditIngestionDetailQuery(
    string PublicId)
    : IRequest<Result<AuditIngestionDetailResult>>;
