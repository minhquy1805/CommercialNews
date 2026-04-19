using Authorization.Application.Contracts.Roles;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.Roles;

public static class DeactivateRoleValidator
{
    public static Error? Validate(DeactivateRoleRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (request.RoleId <= 0)
        {
            return AuthorizationErrors.Role.InvalidRoleId;
        }

        return null;
    }
}