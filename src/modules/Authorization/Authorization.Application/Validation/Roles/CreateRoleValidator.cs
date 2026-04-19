using Authorization.Application.Contracts.Roles;
using Authorization.Application.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Validation.Roles;

public static class CreateRoleValidator
{
    private const int MaxNameLength = 80;
    private const int MaxDisplayNameLength = 120;
    private const int MaxDescriptionLength = 300;

    public static Error? Validate(CreateRoleRequestDto? request)
    {
        if (request is null)
        {
            return AuthorizationErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return AuthorizationErrors.Role.NameRequired;
        }

        if (request.Name.Trim().Length > MaxNameLength)
        {
            return AuthorizationErrors.Role.NameTooLong;
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName) &&
            request.DisplayName.Trim().Length > MaxDisplayNameLength)
        {
            return AuthorizationErrors.Role.DisplayNameTooLong;
        }

        if (!string.IsNullOrWhiteSpace(request.Description) &&
            request.Description.Trim().Length > MaxDescriptionLength)
        {
            return AuthorizationErrors.Role.DescriptionTooLong;
        }

        return null;
    }
}