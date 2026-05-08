using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.GetUserSecuritySummary;
using Identity.Application.Errors;

namespace Identity.Application.Validation.Users.GetUserSecuritySummary;

public static class GetUserSecuritySummaryValidator
{
    public static Error? Validate(GetUserSecuritySummaryRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        if (request.UserId <= 0)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        return null;
    }
}