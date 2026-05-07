using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.GetUserDetail;
using Identity.Application.Errors;

namespace Identity.Application.Validation.Users.GetUserDetail;

public static class GetUserDetailValidator
{
    public static Error? Validate(GetUserDetailRequestDto? request)
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