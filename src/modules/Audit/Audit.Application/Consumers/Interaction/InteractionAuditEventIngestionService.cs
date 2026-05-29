using System.Text.Json;
using Audit.Application.Consumers.Interaction.Payloads;
using Audit.Application.Contracts.Ingestion;
using Audit.Application.Services;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Interaction;

public sealed class InteractionAuditEventIngestionService
    : IInteractionAuditEventIngestionService
{
    private const string SourceModule = "Interaction";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAuditIngestionService _auditIngestionService;

    public InteractionAuditEventIngestionService(
        IAuditIngestionService auditIngestionService)
    {
        _auditIngestionService = auditIngestionService
            ?? throw new ArgumentNullException(nameof(auditIngestionService));
    }

    public Task<Result<AuditIngestionResult>> IngestCommentHiddenAsync(
        InteractionAuditEnvelopeContext context,
        CommentHiddenAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "CommentHidden",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ModeratorUserId,
                Outcome = "Success",
                Summary =
                    $"Comment '{payload.CommentPublicId}' was hidden through '{payload.ResolutionSource}' moderation.",
                Reason = payload.ReasonCode,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                NewValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(context)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestCommentRestoredAsync(
        InteractionAuditEnvelopeContext context,
        CommentRestoredAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "CommentRestored",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ModeratorUserId,
                Outcome = "Success",
                Summary =
                    $"Comment '{payload.CommentPublicId}' was restored to public visibility.",
                Reason = null,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                NewValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(context)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestCommentDeletedByAuthorAsync(
        InteractionAuditEnvelopeContext context,
        CommentDeletedByAuthorAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "CommentDeletedByAuthor",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.AuthorUserId,
                Outcome = "Success",
                Summary =
                    $"Comment '{payload.CommentPublicId}' was deleted by its author.",
                Reason = null,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                NewValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(context)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestCommentReportsDismissedAsync(
        InteractionAuditEnvelopeContext context,
        CommentReportsDismissedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "CommentReportsDismissed",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ModeratorUserId,
                Outcome = "Success",
                Summary =
                    $"Comment moderation case '{payload.CommentModerationCasePublicId}' was dismissed.",
                Reason = payload.ReasonCode,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                NewValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(context)
            },
            cancellationToken);
    }

    private static string SerializePayload<TPayload>(TPayload payload)
    {
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string BuildMetadataJson(
        InteractionAuditEnvelopeContext context)
    {
        var metadata = new
        {
            sourceModule = SourceModule,
            context.EventType,
            context.AggregateType,
            context.AggregateId,
            context.AggregatePublicId,
            context.AggregateVersion,
            context.CorrelationId,
            context.InitiatorUserId,
            context.OccurredAtUtc
        };

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }
}
