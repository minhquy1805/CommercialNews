using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.VerifyEmail;
using Identity.Application.Errors;

namespace Identity.Application.Validation.VerifyEmail;

public static class VerifyEmailValidator
{
    public static Error? Validate(VerifyEmailRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return IdentityErrors.ValidationFailed;
        }

        return null;
    }
}