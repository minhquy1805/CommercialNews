using System.Text.Json;
using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using Audit.Application.Services;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Content;

public sealed class ContentAuditEventIngestionService
    : IContentAuditEventIngestionService
{
    private const string SourceModule = "Content";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAuditIngestionService _auditIngestionService;

    public ContentAuditEventIngestionService(
        IAuditIngestionService auditIngestionService)
    {
        _auditIngestionService = auditIngestionService
            ?? throw new ArgumentNullException(nameof(auditIngestionService));
    }

    public Task<Result<AuditIngestionResult>> IngestArticleCreatedAsync(
        ContentAuditEnvelopeContext context,
        ArticleCreatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleCreated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.CreatedByUserId,
                Outcome = "Success",
                Summary = $"Article '{payload.ArticlePublicId}' was created as '{payload.Status}'.",
                Reason = null,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                NewValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(
                    context,
                    payload.BusinessDedupeKey,
                    sourceModule: SourceModule)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestArticleUpdatedAsync(
        ContentAuditEnvelopeContext context,
        ArticleUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleUpdated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Article '{payload.ArticlePublicId}' was updated to version '{payload.Version}'.",
                Reason = payload.ChangeSummary,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                NewValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(
                    context,
                    payload.BusinessDedupeKey,
                    sourceModule: SourceModule)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestArticlePublishedAsync(
        ContentAuditEnvelopeContext context,
        ArticlePublishedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticlePublished",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Article '{payload.ArticlePublicId}' was published.",
                Reason = null,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                NewValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(
                    context,
                    payload.BusinessDedupeKey,
                    sourceModule: SourceModule)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestArticleUnpublishedAsync(
        ContentAuditEnvelopeContext context,
        ArticleUnpublishedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleUnpublished",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Article '{payload.ArticlePublicId}' was unpublished.",
                Reason = payload.Reason,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                OldValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(
                    context,
                    payload.BusinessDedupeKey,
                    sourceModule: SourceModule)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestArticleArchivedAsync(
        ContentAuditEnvelopeContext context,
        ArticleArchivedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleArchived",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Article '{payload.ArticlePublicId}' was archived.",
                Reason = null,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                OldValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(
                    context,
                    payload.BusinessDedupeKey,
                    sourceModule: SourceModule)
            },
            cancellationToken);
    }

    public Task<Result<AuditIngestionResult>> IngestArticleSoftDeletedAsync(
        ContentAuditEnvelopeContext context,
        ArticleSoftDeletedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleSoftDeleted",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Article '{payload.ArticlePublicId}' was soft-deleted.",
                Reason = null,
                OccurredAtUtc = context.OccurredAtUtc,
                CorrelationId = context.CorrelationId,
                OldValuesJson = SerializePayload(payload),
                MetadataJson = BuildMetadataJson(
                    context,
                    payload.BusinessDedupeKey,
                    sourceModule: SourceModule)
            },
            cancellationToken);
    }

    private static string SerializePayload<TPayload>(TPayload payload)
    {
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string BuildMetadataJson(
        ContentAuditEnvelopeContext context,
        string businessDedupeKey,
        string sourceModule)
    {
        var metadata = new
        {
            sourceModule,
            context.EventType,
            context.AggregateType,
            context.AggregateId,
            context.AggregatePublicId,
            context.AggregateVersion,
            context.CorrelationId,
            context.InitiatorUserId,
            context.OccurredAtUtc,
            businessDedupeKey
        };

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }
}