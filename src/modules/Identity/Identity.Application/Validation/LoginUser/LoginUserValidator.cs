using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.LoginUser;
using Identity.Application.Errors;

namespace Identity.Application.Validation.LoginUser;

public static class LoginUserValidator
{
    public static Error? Validate(LoginUserRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return IdentityErrors.ValidationFailed;
        }

        return null;
    }
}