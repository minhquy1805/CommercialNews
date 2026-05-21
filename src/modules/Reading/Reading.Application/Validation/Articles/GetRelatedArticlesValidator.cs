using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Errors;

namespace Reading.Application.Validation.Articles;

public static class GetRelatedArticlesValidator
{
    private const int PublicIdLength = 26;
    private const int MaxLimit = 20;

    public static Error? Validate(GetRelatedArticlesRequest? request)
    {
        if (request is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.ArticlePublicId))
        {
            return ReadingErrors.Article.InvalidArticlePublicId;
        }

        if (request.ArticlePublicId.Trim().Length != PublicIdLength)
        {
            return ReadingErrors.Article.InvalidArticlePublicId;
        }

        if (request.Limit <= 0)
        {
            return ReadingErrors.Query.InvalidLimit;
        }

        if (request.Limit > MaxLimit)
        {
            return ReadingErrors.Query.LimitTooLarge;
        }

        return null;
    }

    public static string NormalizeArticlePublicId(string articlePublicId)
    {
        return articlePublicId.Trim();
    }
}