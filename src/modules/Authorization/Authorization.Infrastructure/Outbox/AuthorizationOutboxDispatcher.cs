using Authorization.Infrastructure.Outbox;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Infrastructure.Outbox;

public sealed class AuthorizationOutboxDispatcher : IOutboxDispatcher
{
    public Task<Result<DispatchOutboxMessageResult>> DispatchAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        if (outboxMessage is null)
        {
            return Task.FromResult(
                Result<DispatchOutboxMessageResult>.Failure(
                    OutboxErrors.InvalidRequest));
        }

        return outboxMessage.EventType switch
        {
            AuthorizationOutboxEventTypes.RoleCreated => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.RoleUpdated => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.RoleActivated => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.RoleDeactivated => DispatchSupportedAsync(),

            AuthorizationOutboxEventTypes.PermissionCreated => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.PermissionUpdated => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.PermissionActivated => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.PermissionDeactivated => DispatchSupportedAsync(),

            AuthorizationOutboxEventTypes.UserRoleAssigned => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.UserRoleRevoked => DispatchSupportedAsync(),

            AuthorizationOutboxEventTypes.RolePermissionGranted => DispatchSupportedAsync(),
            AuthorizationOutboxEventTypes.RolePermissionRevoked => DispatchSupportedAsync(),

            _ => Task.FromResult(
                Result<DispatchOutboxMessageResult>.Failure(
                    OutboxErrors.Message.UnsupportedEventType))
        };
    }

    private static Task<Result<DispatchOutboxMessageResult>> DispatchSupportedAsync()
    {
        return Task.FromResult(
            Result<DispatchOutboxMessageResult>.Success(
                new DispatchOutboxMessageResult
                {
                    Succeeded = true,
                    ShouldMarkDead = false
                }));
    }
}