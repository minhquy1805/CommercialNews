using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.RegisterUser;
using Identity.Application.Errors;

namespace Identity.Application.Validation.RegisterUser;

public static class RegisterUserValidator
{
    private const int PasswordMinLength = 12;

    public static Error? Validate(RegisterUserRequestDto? request)
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
            return IdentityErrors.PasswordPolicyViolation;
        }

        if (request.Password.Trim().Length < PasswordMinLength)
        {
            return IdentityErrors.PasswordPolicyViolation;
        }

        return null;
    }
}