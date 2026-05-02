using System.Text.Json;
using Audit.Application.Consumers.Authorization.Payloads;
using Audit.Application.Contracts.Ingestion;
using Audit.Application.Services;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Authorization;

public sealed class AuthorizationAuditEventIngestionService
    : IAuthorizationAuditEventIngestionService
{
    private const string SourceModule = "Authorization";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAuditIngestionService _auditIngestionService;

    public AuthorizationAuditEventIngestionService(
        IAuditIngestionService auditIngestionService)
    {
        _auditIngestionService = auditIngestionService
            ?? throw new ArgumentNullException(nameof(auditIngestionService));
    }

    public Task<Result<AuditIngestionResult>> IngestUserRoleAssignedAsync(
        AuthorizationAuditEnvelopeContext context,
        UserRoleAssignedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "UserRoleAssigned",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.AssignedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Role '{payload.RoleName}' was assigned to user '{payload.UserId}'.",
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

    public Task<Result<AuditIngestionResult>> IngestUserRoleRevokedAsync(
        AuthorizationAuditEnvelopeContext context,
        UserRoleRevokedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "UserRoleRevoked",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.RevokedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Role '{payload.RoleName}' was revoked from user '{payload.UserId}'.",
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

    public Task<Result<AuditIngestionResult>> IngestRolePermissionGrantedAsync(
        AuthorizationAuditEnvelopeContext context,
        RolePermissionGrantedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "RolePermissionGranted",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.GrantedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Permission '{payload.PermissionKey}' was granted to role '{payload.RoleName}'.",
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

    public Task<Result<AuditIngestionResult>> IngestRolePermissionRevokedAsync(
        AuthorizationAuditEnvelopeContext context,
        RolePermissionRevokedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "RolePermissionRevoked",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.RevokedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Permission '{payload.PermissionKey}' was revoked from role '{payload.RoleName}'.",
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

    public Task<Result<AuditIngestionResult>> IngestRoleCreatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleCreatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "RoleCreated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.CreatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Role '{payload.RoleName}' was created.",
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

    public Task<Result<AuditIngestionResult>> IngestRoleUpdatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "RoleUpdated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.UpdatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Role '{payload.RoleName}' was updated.",
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

    public Task<Result<AuditIngestionResult>> IngestRoleActivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleActivatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "RoleActivated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActivatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Role '{payload.RoleName}' was activated.",
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

    public Task<Result<AuditIngestionResult>> IngestRoleDeactivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleDeactivatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "RoleDeactivated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.DeactivatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Role '{payload.RoleName}' was deactivated.",
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

    public Task<Result<AuditIngestionResult>> IngestPermissionCreatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionCreatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "PermissionCreated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.CreatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Permission '{payload.PermissionKey}' was created.",
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

    public Task<Result<AuditIngestionResult>> IngestPermissionUpdatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "PermissionUpdated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.UpdatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Permission '{payload.PermissionKey}' was updated.",
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

    public Task<Result<AuditIngestionResult>> IngestPermissionActivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionActivatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "PermissionActivated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.ActivatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Permission '{payload.PermissionKey}' was activated.",
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

    public Task<Result<AuditIngestionResult>> IngestPermissionDeactivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionDeactivatedAuditPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return _auditIngestionService.IngestAsync(
            new AuditIngestionRequest
            {
                MessageId = context.MessageId,
                Action = "PermissionDeactivated",
                ResourceType = context.AggregateType,
                ResourceId = context.AggregateId,
                ActorUserId = payload.DeactivatedByUserId ?? context.InitiatorUserId,
                Outcome = "Success",
                Summary = $"Permission '{payload.PermissionKey}' was deactivated.",
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
        AuthorizationAuditEnvelopeContext context,
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