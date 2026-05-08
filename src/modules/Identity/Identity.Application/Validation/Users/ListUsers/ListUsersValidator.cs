using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.ListUsers;
using Identity.Application.Errors;
using Identity.Domain.Enums;

namespace Identity.Application.Validation.Users.ListUsers;

public static class ListUsersValidator
{
    private const int QueryMaxLength = 320;
    private const int StatusMaxLength = 20;
    private const int MaxPageSize = 100;
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    public static Error? Validate(ListUsersRequestDto? request)
    {
        if (request is null)
        {
            return IdentityErrors.User.InvalidRequest;
        }

        if (request.Page <= 0)
        {
            return IdentityErrors.User.InvalidPaging;
        }

        if (request.PageSize <= 0 || request.PageSize > MaxPageSize)
        {
            return IdentityErrors.User.InvalidPaging;
        }

        if (request.FromCreatedAt.HasValue &&
            request.ToCreatedAt.HasValue &&
            request.FromCreatedAt.Value > request.ToCreatedAt.Value)
        {
            return IdentityErrors.User.InvalidDateRange;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            string status = request.Status.Trim();

            if (status.Length > StatusMaxLength)
            {
                return IdentityErrors.User.InvalidStatus;
            }

            if (!UserAccountStatuses.IsValid(status))
            {
                return IdentityErrors.User.InvalidStatus;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Query) &&
            request.Query.Trim().Length > QueryMaxLength)
        {
            return IdentityErrors.User.QueryTooLong;
        }

        return null;
    }

    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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