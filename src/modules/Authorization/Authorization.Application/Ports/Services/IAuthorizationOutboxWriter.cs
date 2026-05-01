using Authorization.Application.Ports.Persistence;

namespace Authorization.Application.Ports.Services;

public interface IAuthorizationOutboxWriter
{
    Task<long> EnqueueUserRoleAssignedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueueUserRoleRevokedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueueRolePermissionGrantedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueueRolePermissionRevokedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueueRoleCreatedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueueRoleUpdatedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueueRoleActivatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long? activatedByUserId,
        DateTime activatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueRoleDeactivatedAsync(
        IAuthorizationUnitOfWork unitOfWork,
        long roleId,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        long? deactivatedByUserId,
        DateTime deactivatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueuePermissionCreatedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueuePermissionUpdatedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueuePermissionActivatedAsync(
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
        CancellationToken cancellationToken = default);

    Task<long> EnqueuePermissionDeactivatedAsync(
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
        CancellationToken cancellationToken = default);
}