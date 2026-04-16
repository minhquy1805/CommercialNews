using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ResendVerificationEmail;
using Identity.Application.Errors;

namespace Identity.Application.Validation.ResendVerificationEmail;

public static class ResendVerificationEmailValidator
{
    public static Error? Validate(ResendVerificationEmailRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.ResendVerification.InvalidRequest;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return IdentityErrors.ResendVerification.InvalidRequest;
        }

        return null;
    }
}