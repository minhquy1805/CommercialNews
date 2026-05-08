using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.GetUserSessions;
using Identity.Application.Errors;

namespace Identity.Application.Validation.Users.GetUserSessions;

public static class GetUserSessionsValidator
{
    public static Error? Validate(GetUserSessionsRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.Session.InvalidRequest;
        }

        if (request.UserId <= 0)
        {
            return IdentityErrors.Session.InvalidRequest;
        }

        return null;
    }
}