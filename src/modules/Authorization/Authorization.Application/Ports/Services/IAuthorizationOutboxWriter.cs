using Authorization.Application.Contracts.Outbox.Payload;

namespace Authorization.Application.Ports.Services;

public interface IAuthorizationOutboxWriter
{
    Task EnqueueRoleCreatedAsync(
        RoleCreatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueueRoleUpdatedAsync(
        RoleUpdatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueueRoleActivatedAsync(
        RoleActivatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueueRoleDeactivatedAsync(
        RoleDeactivatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueuePermissionCreatedAsync(
        PermissionCreatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueuePermissionUpdatedAsync(
        PermissionUpdatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueuePermissionActivatedAsync(
        PermissionActivatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueuePermissionDeactivatedAsync(
        PermissionDeactivatedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueueUserRoleAssignedAsync(
        UserRoleAssignedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueueUserRoleRevokedAsync(
        UserRoleRevokedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueueRolePermissionGrantedAsync(
        RolePermissionGrantedOutboxPayload payload,
        CancellationToken cancellationToken = default);

    Task EnqueueRolePermissionRevokedAsync(
        RolePermissionRevokedOutboxPayload payload,
        CancellationToken cancellationToken = default);
}