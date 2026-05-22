using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Errors;

namespace Reading.Application.Validation.Articles;

public static class GetArticleByPublicIdValidator
{
    private const int PublicIdLength = 26;

    public static Error? Validate(GetArticleByPublicIdRequest? request)
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

        return null;
    }

    public static string NormalizeArticlePublicId(string articlePublicId)
    {
        return articlePublicId.Trim();
    }
}