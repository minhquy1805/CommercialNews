using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.LoginHistory.GetUserLoginHistory;
using Identity.Application.Errors;

namespace Identity.Application.Validation.LoginHistory.GetUserLoginHistory;

public static class GetUserLoginHistoryValidator
{
    private const int MaxPageSize = 100;
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    public static Error? Validate(GetUserLoginHistoryRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.LoginHistory.InvalidRequest;
        }

        if (request.UserId <= 0)
        {
            return IdentityErrors.LoginHistory.InvalidRequest;
        }

        if (request.Page <= 0)
        {
            return IdentityErrors.LoginHistory.InvalidPaging;
        }

        if (request.PageSize <= 0 || request.PageSize > MaxPageSize)
        {
            return IdentityErrors.LoginHistory.InvalidPaging;
        }

        if (request.FromAttemptedAt.HasValue &&
            request.ToAttemptedAt.HasValue &&
            request.FromAttemptedAt.Value > request.ToAttemptedAt.Value)
        {
            return IdentityErrors.LoginHistory.InvalidDateRange;
        }

        return null;
    }

    public static int NormalizePage(int page)
    {
        return page <= 0 ? DefaultPage : page;
    }

    public static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);
    }
}