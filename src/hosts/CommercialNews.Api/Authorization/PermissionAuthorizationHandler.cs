using System.Security.Claims;
using Authorization.Application.Ports.Persistence;
using Microsoft.AspNetCore.Authorization;

namespace CommercialNews.Api.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IAuthorizationPermissionQueryRepository _authorizationPermissionQueryRepository;

    public PermissionAuthorizationHandler(
        IAuthorizationPermissionQueryRepository authorizationPermissionQueryRepository)
    {
        _authorizationPermissionQueryRepository = authorizationPermissionQueryRepository
            ?? throw new ArgumentNullException(nameof(authorizationPermissionQueryRepository));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim =
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return;
        }

        if (!long.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            return;
        }

        var permissions =
            await _authorizationPermissionQueryRepository.GetEffectivePermissionsByUserIdAsync(
                userId,
                CancellationToken.None);

        var hasPermission = permissions.Any(x =>
            string.Equals(x.Key, requirement.PermissionKey, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}