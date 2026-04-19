using Authorization.Application.Contracts.RolePermissions;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.RolePermissions;

public static class GetRolePermissionsValidator
{
    public static Error? Validate(GetRolePermissionsRequestDto? request)
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