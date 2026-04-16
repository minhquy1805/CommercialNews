using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ForgotPassword;
using Identity.Application.Errors;

namespace Identity.Application.Validation.ForgotPassword;

public static class ForgotPasswordValidator
{
    public static Error? Validate(ForgotPasswordRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return IdentityErrors.ValidationFailed;
        }

        return null;
    }
}