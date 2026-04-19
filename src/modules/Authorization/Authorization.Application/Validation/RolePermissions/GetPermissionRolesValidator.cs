using Authorization.Application.Contracts.RolePermissions;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.RolePermissions;

public static class GetPermissionRolesValidator
{
    public static Error? Validate(GetPermissionRolesRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (request.PermissionId <= 0)
        {
            return AuthorizationErrors.Permission.InvalidPermissionId;
        }

        return null;
    }
}