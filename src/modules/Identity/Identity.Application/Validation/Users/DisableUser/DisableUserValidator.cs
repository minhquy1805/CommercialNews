using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.DisableUser;
using Identity.Application.Errors;

namespace Identity.Application.Validation.Users.DisableUser;

public static class DisableUserValidator
{
    private const int ReasonMaxLength = 500;

    public static Error? Validate(DisableUserRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        if (request.UserId <= 0)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason) &&
            request.Reason.Trim().Length > ReasonMaxLength)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        return null;
    }

    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}