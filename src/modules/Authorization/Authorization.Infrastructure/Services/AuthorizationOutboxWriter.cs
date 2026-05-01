using System.Text.Json;
using Authorization.Application.Outbox;
using Authorization.Application.Outbox.Payloads;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;

namespace Authorization.Infrastructure.Services;

public sealed class AuthorizationOutboxWriter : IAuthorizationOutboxWriter
{
    private const string AggregateTypeRole = "Authorization.Role";
    private const string AggregateTypePermission = "Authorization.Permission";
    private const string AggregateTypeUserRole = "Authorization.UserRole";
    private const string AggregateTypeRolePermission = "Authorization.RolePermission";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public AuthorizationOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<long> EnqueueUserRoleAssignedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long userId,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long? assignedByUserId,
        DateTime assignedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateUserRoleEnvelope(
            userId,
            roleId,
            rolePublicId,
            roleName,
            assignedAtUtc);

        string businessDedupeKey =
            $"authorization:user-role-assigned:{userId}:{roleId}";

        var payload = new UserRoleAssignedIntegrationEventPayload(
            UserId: userId,
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleIsSystem: roleIsSystem,
            AssignedByUserId: assignedByUserId,
            AssignedAtUtc: assignedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.UserRoleAssigned,
            AggregateTypeUserRole,
            BuildUserRoleAggregateId(userId, roleId),
            rolePublicId,
            payload,
            assignedAtUtc,
            priority: 2,
            correlationId,
            assignedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueueUserRoleRevokedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long userId,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long? revokedByUserId,
        DateTime revokedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateUserRoleEnvelope(
            userId,
            roleId,
            rolePublicId,
            roleName,
            revokedAtUtc);

        string businessDedupeKey =
            $"authorization:user-role-revoked:{userId}:{roleId}";

        var payload = new UserRoleRevokedIntegrationEventPayload(
            UserId: userId,
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleIsSystem: roleIsSystem,
            RevokedByUserId: revokedByUserId,
            RevokedAtUtc: revokedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.UserRoleRevoked,
            AggregateTypeUserRole,
            BuildUserRoleAggregateId(userId, roleId),
            rolePublicId,
            payload,
            revokedAtUtc,
            priority: 1,
            correlationId,
            revokedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueueRolePermissionGrantedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        string? permissionModule,
        string? permissionAction,
        bool permissionIsSystem,
        long? grantedByUserId,
        DateTime grantedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateRolePermissionEnvelope(
            roleId,
            rolePublicId,
            roleName,
            permissionId,
            permissionPublicId,
            permissionKey,
            grantedAtUtc);

        string businessDedupeKey =
            $"authorization:role-permission-granted:{roleId}:{permissionId}";

        var payload = new RolePermissionGrantedIntegrationEventPayload(
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleIsSystem: roleIsSystem,
            PermissionId: permissionId,
            PermissionPublicId: permissionPublicId.Trim(),
            PermissionKey: permissionKey.Trim(),
            PermissionModule: NormalizeOptional(permissionModule),
            PermissionAction: NormalizeOptional(permissionAction),
            PermissionIsSystem: permissionIsSystem,
            GrantedByUserId: grantedByUserId,
            GrantedAtUtc: grantedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.RolePermissionGranted,
            AggregateTypeRolePermission,
            BuildRolePermissionAggregateId(roleId, permissionId),
            rolePublicId,
            payload,
            grantedAtUtc,
            priority: 2,
            correlationId,
            grantedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueueRolePermissionRevokedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        string? permissionModule,
        string? permissionAction,
        bool permissionIsSystem,
        long? revokedByUserId,
        DateTime revokedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateRolePermissionEnvelope(
            roleId,
            rolePublicId,
            roleName,
            permissionId,
            permissionPublicId,
            permissionKey,
            revokedAtUtc);

        string businessDedupeKey =
            $"authorization:role-permission-revoked:{roleId}:{permissionId}";

        var payload = new RolePermissionRevokedIntegrationEventPayload(
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleIsSystem: roleIsSystem,
            PermissionId: permissionId,
            PermissionPublicId: permissionPublicId.Trim(),
            PermissionKey: permissionKey.Trim(),
            PermissionModule: NormalizeOptional(permissionModule),
            PermissionAction: NormalizeOptional(permissionAction),
            PermissionIsSystem: permissionIsSystem,
            RevokedByUserId: revokedByUserId,
            RevokedAtUtc: revokedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.RolePermissionRevoked,
            AggregateTypeRolePermission,
            BuildRolePermissionAggregateId(roleId, permissionId),
            rolePublicId,
            payload,
            revokedAtUtc,
            priority: 1,
            correlationId,
            revokedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueueRoleCreatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string roleNameNormalized,
        string? roleDisplayName,
        string? roleDescription,
        bool roleIsSystem,
        bool roleIsActive,
        long? createdByUserId,
        DateTime createdAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateRoleEnvelope(
            roleId,
            rolePublicId,
            roleName,
            roleNameNormalized,
            createdAtUtc);

        string businessDedupeKey =
            $"authorization:role-created:{roleId}";

        var payload = new RoleCreatedIntegrationEventPayload(
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleNameNormalized: roleNameNormalized.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleDescription: NormalizeOptional(roleDescription),
            RoleIsSystem: roleIsSystem,
            RoleIsActive: roleIsActive,
            CreatedByUserId: createdByUserId,
            CreatedAtUtc: createdAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.RoleCreated,
            AggregateTypeRole,
            roleId.ToString(),
            rolePublicId,
            payload,
            createdAtUtc,
            priority: 3,
            correlationId,
            createdByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueueRoleUpdatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string roleNameNormalized,
        string? roleDisplayName,
        string? roleDescription,
        bool roleIsSystem,
        bool roleIsActive,
        long? updatedByUserId,
        DateTime updatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateRoleEnvelope(
            roleId,
            rolePublicId,
            roleName,
            roleNameNormalized,
            updatedAtUtc);

        string businessDedupeKey =
            $"authorization:role-updated:{roleId}:{updatedAtUtc.Ticks}";

        var payload = new RoleUpdatedIntegrationEventPayload(
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleNameNormalized: roleNameNormalized.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleDescription: NormalizeOptional(roleDescription),
            RoleIsSystem: roleIsSystem,
            RoleIsActive: roleIsActive,
            UpdatedByUserId: updatedByUserId,
            UpdatedAtUtc: updatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.RoleUpdated,
            AggregateTypeRole,
            roleId.ToString(),
            rolePublicId,
            payload,
            updatedAtUtc,
            priority: 3,
            correlationId,
            updatedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueueRoleActivatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long? activatedByUserId,
        DateTime activatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateRoleLifecycleEnvelope(
            roleId,
            rolePublicId,
            roleName,
            activatedAtUtc);

        string businessDedupeKey =
            $"authorization:role-activated:{roleId}";

        var payload = new RoleActivatedIntegrationEventPayload(
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleIsSystem: roleIsSystem,
            ActivatedByUserId: activatedByUserId,
            ActivatedAtUtc: activatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.RoleActivated,
            AggregateTypeRole,
            roleId.ToString(),
            rolePublicId,
            payload,
            activatedAtUtc,
            priority: 2,
            correlationId,
            activatedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueueRoleDeactivatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long? deactivatedByUserId,
        DateTime deactivatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateRoleLifecycleEnvelope(
            roleId,
            rolePublicId,
            roleName,
            deactivatedAtUtc);

        string businessDedupeKey =
            $"authorization:role-deactivated:{roleId}";

        var payload = new RoleDeactivatedIntegrationEventPayload(
            RoleId: roleId,
            RolePublicId: rolePublicId.Trim(),
            RoleName: roleName.Trim(),
            RoleDisplayName: NormalizeOptional(roleDisplayName),
            RoleIsSystem: roleIsSystem,
            DeactivatedByUserId: deactivatedByUserId,
            DeactivatedAtUtc: deactivatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.RoleDeactivated,
            AggregateTypeRole,
            roleId.ToString(),
            rolePublicId,
            payload,
            deactivatedAtUtc,
            priority: 1,
            correlationId,
            deactivatedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueuePermissionCreatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        string permissionKeyNormalized,
        string? permissionModule,
        string? permissionAction,
        string? permissionDescription,
        bool permissionIsSystem,
        bool permissionIsActive,
        long? createdByUserId,
        DateTime createdAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidatePermissionEnvelope(
            permissionId,
            permissionPublicId,
            permissionKey,
            permissionKeyNormalized,
            createdAtUtc);

        string businessDedupeKey =
            $"authorization:permission-created:{permissionId}";

        var payload = new PermissionCreatedIntegrationEventPayload(
            PermissionId: permissionId,
            PermissionPublicId: permissionPublicId.Trim(),
            PermissionKey: permissionKey.Trim(),
            PermissionKeyNormalized: permissionKeyNormalized.Trim(),
            PermissionModule: NormalizeOptional(permissionModule),
            PermissionAction: NormalizeOptional(permissionAction),
            PermissionDescription: NormalizeOptional(permissionDescription),
            PermissionIsSystem: permissionIsSystem,
            PermissionIsActive: permissionIsActive,
            CreatedByUserId: createdByUserId,
            CreatedAtUtc: createdAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.PermissionCreated,
            AggregateTypePermission,
            permissionId.ToString(),
            permissionPublicId,
            payload,
            createdAtUtc,
            priority: 3,
            correlationId,
            createdByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueuePermissionUpdatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        string permissionKeyNormalized,
        string? permissionModule,
        string? permissionAction,
        string? permissionDescription,
        bool permissionIsSystem,
        bool permissionIsActive,
        long? updatedByUserId,
        DateTime updatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidatePermissionEnvelope(
            permissionId,
            permissionPublicId,
            permissionKey,
            permissionKeyNormalized,
            updatedAtUtc);

        string businessDedupeKey =
            $"authorization:permission-updated:{permissionId}:{updatedAtUtc.Ticks}";

        var payload = new PermissionUpdatedIntegrationEventPayload(
            PermissionId: permissionId,
            PermissionPublicId: permissionPublicId.Trim(),
            PermissionKey: permissionKey.Trim(),
            PermissionKeyNormalized: permissionKeyNormalized.Trim(),
            PermissionModule: NormalizeOptional(permissionModule),
            PermissionAction: NormalizeOptional(permissionAction),
            PermissionDescription: NormalizeOptional(permissionDescription),
            PermissionIsSystem: permissionIsSystem,
            PermissionIsActive: permissionIsActive,
            UpdatedByUserId: updatedByUserId,
            UpdatedAtUtc: updatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.PermissionUpdated,
            AggregateTypePermission,
            permissionId.ToString(),
            permissionPublicId,
            payload,
            updatedAtUtc,
            priority: 3,
            correlationId,
            updatedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueuePermissionActivatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        string? permissionModule,
        string? permissionAction,
        bool permissionIsSystem,
        long? activatedByUserId,
        DateTime activatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidatePermissionLifecycleEnvelope(
            permissionId,
            permissionPublicId,
            permissionKey,
            activatedAtUtc);

        string businessDedupeKey =
            $"authorization:permission-activated:{permissionId}";

        var payload = new PermissionActivatedIntegrationEventPayload(
            PermissionId: permissionId,
            PermissionPublicId: permissionPublicId.Trim(),
            PermissionKey: permissionKey.Trim(),
            PermissionModule: NormalizeOptional(permissionModule),
            PermissionAction: NormalizeOptional(permissionAction),
            PermissionIsSystem: permissionIsSystem,
            ActivatedByUserId: activatedByUserId,
            ActivatedAtUtc: activatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.PermissionActivated,
            AggregateTypePermission,
            permissionId.ToString(),
            permissionPublicId,
            payload,
            activatedAtUtc,
            priority: 2,
            correlationId,
            activatedByUserId,
            cancellationToken);
    }

    public async Task<long> EnqueuePermissionDeactivatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        string? permissionModule,
        string? permissionAction,
        bool permissionIsSystem,
        long? deactivatedByUserId,
        DateTime deactivatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidatePermissionLifecycleEnvelope(
            permissionId,
            permissionPublicId,
            permissionKey,
            deactivatedAtUtc);

        string businessDedupeKey =
            $"authorization:permission-deactivated:{permissionId}";

        var payload = new PermissionDeactivatedIntegrationEventPayload(
            PermissionId: permissionId,
            PermissionPublicId: permissionPublicId.Trim(),
            PermissionKey: permissionKey.Trim(),
            PermissionModule: NormalizeOptional(permissionModule),
            PermissionAction: NormalizeOptional(permissionAction),
            PermissionIsSystem: permissionIsSystem,
            DeactivatedByUserId: deactivatedByUserId,
            DeactivatedAtUtc: deactivatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork,
            AuthorizationIntegrationEventTypes.PermissionDeactivated,
            AggregateTypePermission,
            permissionId.ToString(),
            permissionPublicId,
            payload,
            deactivatedAtUtc,
            priority: 1,
            correlationId,
            deactivatedByUserId,
            cancellationToken);
    }

    private async Task<long> InsertOutboxMessageAsync<TPayload>(
        IAuthorizationUnitOfWork unitOfWork,
        string eventType,
        string aggregateType,
        string aggregateId,
        string? aggregatePublicId,
        TPayload payload,
        DateTime occurredAtUtc,
        byte priority,
        string? correlationId,
        long? initiatorUserId,
        CancellationToken cancellationToken)
    {
        if (!unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Authorization outbox message must be written inside an active transaction.");
        }

        string payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: eventType,
            aggregateType: aggregateType,
            aggregateId: aggregateId,
            payload: payloadJson,
            occurredAt: occurredAtUtc,
            priority: priority,
            aggregatePublicId: NormalizeOptional(aggregatePublicId),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeOptional(correlationId),
            initiatorUserId: initiatorUserId);

        return await _outboxMessageRepository.InsertAsync(
            unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static void ValidateUserRoleEnvelope(
        long userId,
        long roleId,
        string rolePublicId,
        string roleName,
        DateTime occurredAtUtc)
    {
        ValidatePositiveId(userId, nameof(userId));
        ValidatePositiveId(roleId, nameof(roleId));
        ValidateRequired(rolePublicId, nameof(rolePublicId));
        ValidateRequired(roleName, nameof(roleName));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidateRolePermissionEnvelope(
        long roleId,
        string rolePublicId,
        string roleName,
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        DateTime occurredAtUtc)
    {
        ValidatePositiveId(roleId, nameof(roleId));
        ValidateRequired(rolePublicId, nameof(rolePublicId));
        ValidateRequired(roleName, nameof(roleName));

        ValidatePositiveId(permissionId, nameof(permissionId));
        ValidateRequired(permissionPublicId, nameof(permissionPublicId));
        ValidateRequired(permissionKey, nameof(permissionKey));

        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidateRoleEnvelope(
        long roleId,
        string rolePublicId,
        string roleName,
        string roleNameNormalized,
        DateTime occurredAtUtc)
    {
        ValidateRoleLifecycleEnvelope(roleId, rolePublicId, roleName, occurredAtUtc);
        ValidateRequired(roleNameNormalized, nameof(roleNameNormalized));
    }

    private static void ValidateRoleLifecycleEnvelope(
        long roleId,
        string rolePublicId,
        string roleName,
        DateTime occurredAtUtc)
    {
        ValidatePositiveId(roleId, nameof(roleId));
        ValidateRequired(rolePublicId, nameof(rolePublicId));
        ValidateRequired(roleName, nameof(roleName));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidatePermissionEnvelope(
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        string permissionKeyNormalized,
        DateTime occurredAtUtc)
    {
        ValidatePermissionLifecycleEnvelope(
            permissionId,
            permissionPublicId,
            permissionKey,
            occurredAtUtc);

        ValidateRequired(permissionKeyNormalized, nameof(permissionKeyNormalized));
    }

    private static void ValidatePermissionLifecycleEnvelope(
        long permissionId,
        string permissionPublicId,
        string permissionKey,
        DateTime occurredAtUtc)
    {
        ValidatePositiveId(permissionId, nameof(permissionId));
        ValidateRequired(permissionPublicId, nameof(permissionPublicId));
        ValidateRequired(permissionKey, nameof(permissionKey));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidatePositiveId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }
    }

    private static void ValidateRequiredDate(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }
    }

    private static string BuildUserRoleAggregateId(long userId, long roleId)
    {
        return $"{userId}:{roleId}";
    }

    private static string BuildRolePermissionAggregateId(long roleId, long permissionId)
    {
        return $"{roleId}:{permissionId}";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}