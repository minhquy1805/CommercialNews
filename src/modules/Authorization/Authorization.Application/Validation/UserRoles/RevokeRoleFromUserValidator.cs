using Authorization.Application.Contracts.UserRoles;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.UserRoles;

public static class RevokeRoleFromUserValidator
{
    public static Error? Validate(RevokeRoleFromUserRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (request.UserId <= 0)
        {
            return AuthorizationErrors.UserRole.InvalidUserId;
        }

        if (request.RoleId <= 0)
        {
            return AuthorizationErrors.UserRole.InvalidRoleId;
        }

        return null;
    }
}