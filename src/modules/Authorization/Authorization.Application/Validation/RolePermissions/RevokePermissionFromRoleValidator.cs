using Authorization.Application.Contracts.RolePermissions;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.RolePermissions;

public static class RevokePermissionFromRoleValidator
{
    public static Error? Validate(RevokePermissionFromRoleRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (request.RoleId <= 0)
        {
            return AuthorizationErrors.RolePermission.InvalidRoleId;
        }

        if (request.PermissionId <= 0)
        {
            return AuthorizationErrors.RolePermission.InvalidPermissionId;
        }

        return null;
    }
}