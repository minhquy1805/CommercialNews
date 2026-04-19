using Authorization.Application.Contracts.UserRoles;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.UserRoles;

public static class GetUserRolesValidator
{
    public static Error? Validate(GetUserRolesRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (request.UserId <= 0)
        {
            return AuthorizationErrors.UserRole.InvalidUserId;
        }

        return null;
    }
}