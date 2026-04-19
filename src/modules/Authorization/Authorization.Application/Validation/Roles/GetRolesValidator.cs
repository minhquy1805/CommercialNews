using Authorization.Application.Contracts.Roles;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.Roles;

public static class GetRolesValidator
{
    private const int MaxPageSize = 100;

    public static Error? Validate(GetRolesRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (request.Page <= 0 || request.PageSize <= 0 || request.PageSize > MaxPageSize)
        {
            return AuthorizationErrors.Query.InvalidPaging;
        }

        return null;
    }
}