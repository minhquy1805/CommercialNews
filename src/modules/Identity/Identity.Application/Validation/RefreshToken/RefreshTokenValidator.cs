using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.RefreshToken;
using Identity.Application.Errors;

namespace Identity.Application.Validation.RefreshToken;

public static class RefreshTokenValidator
{
    public static Error? Validate(RefreshTokenRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return IdentityErrors.ValidationFailed;
        }

        return null;
    }
}