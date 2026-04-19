using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.Permissions;

public static class CreatePermissionValidator
{
    private const int MaxKeyLength = 120;
    private const int MaxModuleLength = 50;
    private const int MaxActionLength = 50;
    private const int MaxDescriptionLength = 300;

    public static Error? Validate(CreatePermissionRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return AuthorizationErrors.Permission.KeyRequired;
        }

        if (request.Key.Trim().Length > MaxKeyLength)
        {
            return AuthorizationErrors.Permission.KeyTooLong;
        }

        if (!string.IsNullOrWhiteSpace(request.Module) &&
            request.Module.Trim().Length > MaxModuleLength)
        {
            return AuthorizationErrors.Permission.ModuleTooLong;
        }

        if (!string.IsNullOrWhiteSpace(request.Action) &&
            request.Action.Trim().Length > MaxActionLength)
        {
            return AuthorizationErrors.Permission.ActionTooLong;
        }

        if (!string.IsNullOrWhiteSpace(request.Description) &&
            request.Description.Trim().Length > MaxDescriptionLength)
        {
            return AuthorizationErrors.Permission.DescriptionTooLong;
        }

        return null;
    }
}