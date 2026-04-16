using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Logout;
using Identity.Application.Errors;

namespace Identity.Application.Validation.Logout;

public static class LogoutValidator
{
    public static Error? Validate(LogoutRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.Logout.InvalidRequest;
        }

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return IdentityErrors.Logout.InvalidRequest;
        }

        return null;
    }
}