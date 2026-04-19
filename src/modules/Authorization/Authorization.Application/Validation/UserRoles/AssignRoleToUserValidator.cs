using Authorization.Application.Contracts.UserRoles;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.UserRoles;

public static class AssignRoleToUserValidator
{
    public static Error? Validate(AssignRoleToUserRequestDto? request)
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