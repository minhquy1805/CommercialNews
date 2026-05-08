using System.Text.Json;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using Audit.Application.Services;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Identity;

public sealed class IdentityAuditEventIngestionService
    : IIdentityAuditEventIngestionService
{
    private const string SourceModule = "Identity";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAuditIngestionService _auditIngestionService;

    public IdentityAuditEventIngestionService(
        IAuditIngestionService auditIngestionService)
    {
        _auditIngestionService = auditIngestionService
            ?? throw new ArgumentNullException(nameof(auditIngestionService));
    }

    public Task<Result<AuditIngestionResult>> IngestEmailVerifiedAsync(
        IdentityAuditEnvelopeContext context,
        EmailVerifiedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "EmailVerified",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.UserId,
                Outcome = "Success",
                Summary = $"Email was verified for user '{payload.UserId}'.",
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

    public Task<Result<AuditIngestionResult>> IngestPasswordChangedAsync(
        IdentityAuditEnvelopeContext context,
        PasswordChangedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "PasswordChanged",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = context.InitiatorUserId ?? payload.UserId,
                Outcome = "Success",
                Summary = $"Password was changed for user '{payload.UserId}'.",
                Reason = payload.Reason,
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

    public Task<Result<AuditIngestionResult>> IngestUserActivatedAsync(
        IdentityAuditEnvelopeContext context,
        UserActivatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "UserActivated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActorUserId,
                Outcome = "Success",
                Summary = $"User '{payload.TargetUserId}' was activated.",
                Reason = payload.Reason,
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

    public Task<Result<AuditIngestionResult>> IngestUserDisabledAsync(
        IdentityAuditEnvelopeContext context,
        UserDisabledAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "UserDisabled",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActorUserId,
                Outcome = "Success",
                Summary = $"User '{payload.TargetUserId}' was disabled.",
                Reason = payload.Reason,
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

    public Task<Result<AuditIngestionResult>> IngestUserLockedAsync(
        IdentityAuditEnvelopeContext context,
        UserLockedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "UserLocked",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActorUserId,
                Outcome = "Success",
                Summary = $"User '{payload.TargetUserId}' was locked until '{payload.LockedUntilUtc:O}'.",
                Reason = payload.Reason,
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

    public Task<Result<AuditIngestionResult>> IngestUserUnlockedAsync(
        IdentityAuditEnvelopeContext context,
        UserUnlockedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "UserUnlocked",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActorUserId,
                Outcome = "Success",
                Summary = $"User '{payload.TargetUserId}' was unlocked.",
                Reason = payload.Reason,
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

    public Task<Result<AuditIngestionResult>> IngestEmailMarkedVerifiedAsync(
        IdentityAuditEnvelopeContext context,
        EmailMarkedVerifiedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "EmailMarkedVerified",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActorUserId,
                Outcome = "Success",
                Summary = $"Email was marked verified for user '{payload.TargetUserId}'.",
                Reason = payload.Reason,
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

    public Task<Result<AuditIngestionResult>> IngestUserSessionsRevokedAsync(
        IdentityAuditEnvelopeContext context,
        UserSessionsRevokedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "UserSessionsRevoked",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActorUserId,
                Outcome = "Success",
                Summary = $"{payload.RevokedSessionCount} session(s) were revoked for user '{payload.TargetUserId}'.",
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

    private static string SerializePayload<TPayload>(TPayload payload)
    {
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string BuildMetadataJson(
        IdentityAuditEnvelopeContext context,
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
