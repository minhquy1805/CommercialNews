using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.LockUser;
using Identity.Application.Errors;

namespace Identity.Application.Validation.Users.LockUser;

public static class LockUserValidator
{
    private const int ReasonMaxLength = 500;

    public static Error? Validate(
        LockUserRequestDto? request,
        DateTime nowUtc)
    {
        if (request is null)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        if (request.UserId <= 0)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        if (request.LockedUntilUtc == default)
        {
            return IdentityErrors.User.InvalidLockUntil;
        }

        if (request.LockedUntilUtc <= nowUtc)
        {
            return IdentityErrors.User.InvalidLockUntil;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason) &&
            request.Reason.Trim().Length > ReasonMaxLength)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        return null;
    }

    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}