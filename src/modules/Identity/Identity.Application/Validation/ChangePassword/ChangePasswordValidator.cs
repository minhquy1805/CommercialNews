using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ChangePassword;
using Identity.Application.Errors;

namespace Identity.Application.Validation.ChangePassword;

public static class ChangePasswordValidator
{
    private const int MinPasswordLength = 12;

    public static Error? Validate(ChangePasswordRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) ||
            request.NewPassword.Trim().Length < MinPasswordLength)
        {
            return IdentityErrors.PasswordPolicyViolation;
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return IdentityErrors.PasswordPolicyViolation;
        }

        return null;
    }
}