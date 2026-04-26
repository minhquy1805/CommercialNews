using System.Text.Json;
using Authorization.Application.Contracts.Outbox.Payload;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;

namespace Authorization.Infrastructure.Outbox;

public sealed class AuthorizationOutboxWriter : IAuthorizationOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IAuthorizationUnitOfWork _unitOfWork;

    public AuthorizationOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator,
        IAuthorizationUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task EnqueueRoleCreatedAsync(
        RoleCreatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleCreatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.RoleCreated,
            aggregateType: AuthorizationOutboxAggregateTypes.Role,
            aggregateId: BuildRoleAggregateId(payload.RoleId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueueRoleUpdatedAsync(
        RoleUpdatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleUpdatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.RoleUpdated,
            aggregateType: AuthorizationOutboxAggregateTypes.Role,
            aggregateId: BuildRoleAggregateId(payload.RoleId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueueRoleActivatedAsync(
        RoleActivatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleActivatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.RoleActivated,
            aggregateType: AuthorizationOutboxAggregateTypes.Role,
            aggregateId: BuildRoleAggregateId(payload.RoleId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueueRoleDeactivatedAsync(
        RoleDeactivatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleDeactivatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.RoleDeactivated,
            aggregateType: AuthorizationOutboxAggregateTypes.Role,
            aggregateId: BuildRoleAggregateId(payload.RoleId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueuePermissionCreatedAsync(
        PermissionCreatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidatePermissionCreatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.PermissionCreated,
            aggregateType: AuthorizationOutboxAggregateTypes.Permission,
            aggregateId: BuildPermissionAggregateId(payload.PermissionId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.PermissionPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueuePermissionUpdatedAsync(
        PermissionUpdatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidatePermissionUpdatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.PermissionUpdated,
            aggregateType: AuthorizationOutboxAggregateTypes.Permission,
            aggregateId: BuildPermissionAggregateId(payload.PermissionId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.PermissionPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueuePermissionActivatedAsync(
        PermissionActivatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidatePermissionActivatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.PermissionActivated,
            aggregateType: AuthorizationOutboxAggregateTypes.Permission,
            aggregateId: BuildPermissionAggregateId(payload.PermissionId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.PermissionPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueuePermissionDeactivatedAsync(
        PermissionDeactivatedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidatePermissionDeactivatedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.PermissionDeactivated,
            aggregateType: AuthorizationOutboxAggregateTypes.Permission,
            aggregateId: BuildPermissionAggregateId(payload.PermissionId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.PermissionPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueueUserRoleAssignedAsync(
        UserRoleAssignedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateUserRoleAssignedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.UserRoleAssigned,
            aggregateType: AuthorizationOutboxAggregateTypes.UserRole,
            aggregateId: BuildUserRoleAggregateId(payload.UserId, payload.RoleId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueueUserRoleRevokedAsync(
        UserRoleRevokedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateUserRoleRevokedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.UserRoleRevoked,
            aggregateType: AuthorizationOutboxAggregateTypes.UserRole,
            aggregateId: BuildUserRoleAggregateId(payload.UserId, payload.RoleId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueueRolePermissionGrantedAsync(
        RolePermissionGrantedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateRolePermissionGrantedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.RolePermissionGranted,
            aggregateType: AuthorizationOutboxAggregateTypes.RolePermission,
            aggregateId: BuildRolePermissionAggregateId(payload.RoleId, payload.PermissionId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueueRolePermissionRevokedAsync(
        RolePermissionRevokedOutboxPayload payload,
        CancellationToken cancellationToken = default)
    {
        ValidateRolePermissionRevokedPayload(payload);

        var outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: AuthorizationOutboxEventTypes.RolePermissionRevoked,
            aggregateType: AuthorizationOutboxAggregateTypes.RolePermission,
            aggregateId: BuildRolePermissionAggregateId(payload.RoleId, payload.PermissionId),
            payload: JsonSerializer.Serialize(payload, SerializerOptions),
            occurredAt: payload.OccurredAtUtc,
            priority: AuthorizationOutboxDefaults.DefaultPriority,
            aggregatePublicId: payload.RolePublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeNullable(payload.CorrelationId),
            initiatorUserId: payload.ActorUserId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static string BuildRoleAggregateId(long roleId)
    {
        return $"role:{roleId}";
    }

    private static string BuildPermissionAggregateId(long permissionId)
    {
        return $"permission:{permissionId}";
    }

    private static string BuildUserRoleAggregateId(long userId, long roleId)
    {
        return $"user-role:{userId}:{roleId}";
    }

    private static string BuildRolePermissionAggregateId(long roleId, long permissionId)
    {
        return $"role-permission:{roleId}:{permissionId}";
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void ValidateRoleCreatedPayload(RoleCreatedOutboxPayload payload)
    {
        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleNameNormalized))
        {
            throw new ArgumentException("RoleNameNormalized is required.", nameof(payload.RoleNameNormalized));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidateRoleUpdatedPayload(RoleUpdatedOutboxPayload payload)
    {
        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleNameNormalized))
        {
            throw new ArgumentException("RoleNameNormalized is required.", nameof(payload.RoleNameNormalized));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidateRoleActivatedPayload(RoleActivatedOutboxPayload payload)
    {
        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidateRoleDeactivatedPayload(RoleDeactivatedOutboxPayload payload)
    {
        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidatePermissionCreatedPayload(PermissionCreatedOutboxPayload payload)
    {
        if (payload.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.PermissionId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionPublicId))
        {
            throw new ArgumentException("PermissionPublicId is required.", nameof(payload.PermissionPublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKey))
        {
            throw new ArgumentException("PermissionKey is required.", nameof(payload.PermissionKey));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKeyNormalized))
        {
            throw new ArgumentException("PermissionKeyNormalized is required.", nameof(payload.PermissionKeyNormalized));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidatePermissionUpdatedPayload(PermissionUpdatedOutboxPayload payload)
    {
        if (payload.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.PermissionId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionPublicId))
        {
            throw new ArgumentException("PermissionPublicId is required.", nameof(payload.PermissionPublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKey))
        {
            throw new ArgumentException("PermissionKey is required.", nameof(payload.PermissionKey));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKeyNormalized))
        {
            throw new ArgumentException("PermissionKeyNormalized is required.", nameof(payload.PermissionKeyNormalized));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidatePermissionActivatedPayload(PermissionActivatedOutboxPayload payload)
    {
        if (payload.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.PermissionId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionPublicId))
        {
            throw new ArgumentException("PermissionPublicId is required.", nameof(payload.PermissionPublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKey))
        {
            throw new ArgumentException("PermissionKey is required.", nameof(payload.PermissionKey));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidatePermissionDeactivatedPayload(PermissionDeactivatedOutboxPayload payload)
    {
        if (payload.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.PermissionId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionPublicId))
        {
            throw new ArgumentException("PermissionPublicId is required.", nameof(payload.PermissionPublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKey))
        {
            throw new ArgumentException("PermissionKey is required.", nameof(payload.PermissionKey));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidateUserRoleAssignedPayload(UserRoleAssignedOutboxPayload payload)
    {
        if (payload.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.UserId));
        }

        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidateUserRoleRevokedPayload(UserRoleRevokedOutboxPayload payload)
    {
        if (payload.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.UserId));
        }

        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidateRolePermissionGrantedPayload(RolePermissionGrantedOutboxPayload payload)
    {
        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (payload.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.PermissionId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionPublicId))
        {
            throw new ArgumentException("PermissionPublicId is required.", nameof(payload.PermissionPublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKey))
        {
            throw new ArgumentException("PermissionKey is required.", nameof(payload.PermissionKey));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }

    private static void ValidateRolePermissionRevokedPayload(RolePermissionRevokedOutboxPayload payload)
    {
        if (payload.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.RoleId));
        }

        if (payload.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payload.PermissionId));
        }

        if (string.IsNullOrWhiteSpace(payload.RolePublicId))
        {
            throw new ArgumentException("RolePublicId is required.", nameof(payload.RolePublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.RoleName))
        {
            throw new ArgumentException("RoleName is required.", nameof(payload.RoleName));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionPublicId))
        {
            throw new ArgumentException("PermissionPublicId is required.", nameof(payload.PermissionPublicId));
        }

        if (string.IsNullOrWhiteSpace(payload.PermissionKey))
        {
            throw new ArgumentException("PermissionKey is required.", nameof(payload.PermissionKey));
        }

        if (payload.OccurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(payload.OccurredAtUtc));
        }
    }
}