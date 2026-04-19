using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.Permissions;

public static class GetPermissionsValidator
{
    private const int MaxPageSize = 100;
    private const int MaxModuleLength = 50;
    private const int MaxActionLength = 50;

    public static Error? Validate(GetPermissionsRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (request.Page <= 0 || request.PageSize <= 0 || request.PageSize > MaxPageSize)
        {
            return AuthorizationErrors.Query.InvalidPaging;
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

        return null;
    }
}