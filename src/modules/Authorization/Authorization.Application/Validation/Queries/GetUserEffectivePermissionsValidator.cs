using Authorization.Application.Contracts.Queries;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.Queries;

public static class GetUserEffectivePermissionsValidator
{
    public static Error? Validate(GetUserEffectivePermissionsRequestDto? request)
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