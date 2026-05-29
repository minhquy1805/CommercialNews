using Audit.Application.Consumers.Interaction.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Interaction;

public interface IInteractionAuditEventIngestionService
{
    Task<Result<AuditIngestionResult>> IngestCommentHiddenAsync(
        InteractionAuditEnvelopeContext context,
        CommentHiddenAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestCommentRestoredAsync(
        InteractionAuditEnvelopeContext context,
        CommentRestoredAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestCommentDeletedByAuthorAsync(
        InteractionAuditEnvelopeContext context,
        CommentDeletedByAuthorAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestCommentReportsDismissedAsync(
        InteractionAuditEnvelopeContext context,
        CommentReportsDismissedAuditPayload payload,
        CancellationToken cancellationToken = default);
}
