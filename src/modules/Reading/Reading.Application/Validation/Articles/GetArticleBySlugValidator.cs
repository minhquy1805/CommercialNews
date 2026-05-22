using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Errors;

namespace Reading.Application.Validation.Articles;

public static class GetArticleBySlugValidator
{
    private const int MaxSlugLength = 300;

    public static Error? Validate(GetArticleBySlugRequest? request)
    {
        if (request is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return ReadingErrors.Route.SlugRequired;
        }

        if (request.Slug.Trim().Length > MaxSlugLength)
        {
            return ReadingErrors.Route.SlugTooLong;
        }

        return null;
    }

    public static string NormalizeSlug(string slug)
    {
        return slug.Trim();
    }
}