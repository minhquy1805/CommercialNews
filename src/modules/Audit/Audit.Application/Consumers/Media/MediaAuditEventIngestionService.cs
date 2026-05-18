using System.Text.Json;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using Audit.Application.Services;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Media;

public sealed class MediaAuditEventIngestionService
    : IMediaAuditEventIngestionService
{
    private const string SourceModule = "Media";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAuditIngestionService _auditIngestionService;

    public MediaAuditEventIngestionService(
        IAuditIngestionService auditIngestionService)
    {
        _auditIngestionService = auditIngestionService
            ?? throw new ArgumentNullException(nameof(auditIngestionService));
    }

    public Task<Result<AuditIngestionResult>> IngestMediaAssetRegisteredAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetRegisteredAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "MediaAssetRegistered",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media asset '{payload.MediaPublicId}' was registered as '{payload.MediaType}'.",
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

    public Task<Result<AuditIngestionResult>> IngestMediaAssetUpdatedAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "MediaAssetUpdated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media asset '{payload.MediaPublicId}' metadata was updated to version '{payload.Version}'.",
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

    public Task<Result<AuditIngestionResult>> IngestMediaAssetSoftDeletedAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetSoftDeletedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "MediaAssetSoftDeleted",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media asset '{payload.MediaPublicId}' was soft-deleted.",
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

    public Task<Result<AuditIngestionResult>> IngestMediaAssetRestoredAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetRestoredAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "MediaAssetRestored",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media asset '{payload.MediaPublicId}' was restored.",
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

    public Task<Result<AuditIngestionResult>> IngestArticleMediaAttachedAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaAttachedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleMediaAttached",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media '{payload.MediaId}' was attached to article '{payload.ArticleId}'.",
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

    public Task<Result<AuditIngestionResult>> IngestArticleMediaDetachedAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaDetachedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleMediaDetached",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media '{payload.MediaId}' was detached from article '{payload.ArticleId}'.",
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

    public Task<Result<AuditIngestionResult>> IngestArticleMediaReorderedAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaReorderedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticleMediaReordered",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media attachments for article '{payload.ArticleId}' were reordered to version '{payload.AttachmentSetVersion}'.",
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

    public Task<Result<AuditIngestionResult>> IngestArticlePrimaryMediaSetAsync(
        MediaAuditEnvelopeContext context,
        ArticlePrimaryMediaSetAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "ArticlePrimaryMediaSet",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Media '{payload.MediaId}' was set as primary for article '{payload.ArticleId}'.",
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

    private static string SerializePayload<TPayload>(TPayload payload)
    {
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string BuildMetadataJson(
        MediaAuditEnvelopeContext context,
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