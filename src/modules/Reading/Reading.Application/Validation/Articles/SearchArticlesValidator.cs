using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Errors;
using Reading.Domain.Constants;

namespace Reading.Application.Validation.Articles;

public static class SearchArticlesValidator
{
    private const int MaxPageSize = 100;
    private const int MaxQueryLength = 300;

    public static Error? Validate(SearchArticlesRequest? request)
    {
        if (request is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return ReadingErrors.Query.SearchQueryRequired;
        }

        if (request.Query.Trim().Length > MaxQueryLength)
        {
            return ReadingErrors.Query.SearchQueryTooLong;
        }

        if (request.Page < 1)
        {
            return ReadingErrors.Query.InvalidPage;
        }

        if (request.PageSize <= 0)
        {
            return ReadingErrors.Query.InvalidPageSize;
        }

        if (request.PageSize > MaxPageSize)
        {
            return ReadingErrors.Query.PageSizeTooLarge;
        }

        string sort = NormalizeSort(request.Sort);

        if (!ReadingSortValues.IsValid(sort))
        {
            return ReadingErrors.Query.InvalidSort;
        }

        return null;
    }

    public static string NormalizeQuery(string query)
    {
        return query.Trim();
    }

    public static string NormalizeSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return ReadingSortValues.PublishedAtDescending;
        }

        return sort.Trim();
    }
}