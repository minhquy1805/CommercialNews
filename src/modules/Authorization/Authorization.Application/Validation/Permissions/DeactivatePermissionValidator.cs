using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.Permissions;

public static class DeactivatePermissionValidator
{
    public static Error? Validate(DeactivatePermissionRequestDto? request)
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