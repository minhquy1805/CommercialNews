using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ResetPassword;
using Identity.Application.Errors;

namespace Identity.Application.Validation.ResetPassword;

public static class ResetPasswordValidator
{
    private const int MinPasswordLength = 12;

    public static Error? Validate(ResetPasswordRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) ||
            request.NewPassword.Trim().Length < MinPasswordLength)
        {
            return IdentityErrors.PasswordPolicyViolation;
        }

        return null;
    }
}