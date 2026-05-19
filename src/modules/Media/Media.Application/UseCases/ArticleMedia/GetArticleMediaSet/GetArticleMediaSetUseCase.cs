using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.GetArticleMediaSet;

public sealed class GetArticleMediaSetUseCase : IGetArticleMediaSetUseCase
{
    private readonly IArticleMediaSetRepository _articleMediaSetRepository;

    public GetArticleMediaSetUseCase(
        IArticleMediaSetRepository articleMediaSetRepository)
    {
        _articleMediaSetRepository = articleMediaSetRepository
            ?? throw new ArgumentNullException(nameof(articleMediaSetRepository));
    }

    public async Task<Result<GetArticleMediaSetResponse>> ExecuteAsync(
        GetArticleMediaSetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetArticleMediaSetResponse>.Failure(
                    MediaErrors.ArticleMediaSet.InvalidArticleId);
            }

            ArticleMediaSet? articleMediaSet =
                await _articleMediaSetRepository.GetByArticleIdAsync(
                    request.ArticleId,
                    cancellationToken);

            if (articleMediaSet is null)
            {
                return Result<GetArticleMediaSetResponse>.Failure(
                    MediaErrors.ArticleMediaSet.NotFound);
            }

            return Result<GetArticleMediaSetResponse>.Success(
                new GetArticleMediaSetResponse
                {
                    ArticleId = articleMediaSet.ArticleId,
                    Version = articleMediaSet.Version,
                    CreatedAt = articleMediaSet.CreatedAt,
                    CreatedBy = articleMediaSet.CreatedBy,
                    UpdatedAt = articleMediaSet.UpdatedAt,
                    UpdatedBy = articleMediaSet.UpdatedBy
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetArticleMediaSetResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetArticleMediaSetResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_SET_INVALID_ARTICLE_ID" =>
                MediaErrors.ArticleMediaSet.InvalidArticleId,

            "MEDIA.ARTICLE_MEDIA_SET_INVALID_VERSION" =>
                MediaErrors.ArticleMediaSet.InvalidVersion,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_NOT_FOUND" =>
                MediaErrors.Article.NotFound,

            "MEDIA.CONSTRAINT_VIOLATION" =>
                MediaErrors.ConstraintViolation,

            "MEDIA.CONCURRENT_MODIFICATION" =>
                MediaErrors.ConcurrentModification,

            "MEDIA.DEPENDENCY_UNAVAILABLE" =>
                MediaErrors.DependencyUnavailable,

            "MEDIA.PERSISTENCE_ERROR" =>
                MediaErrors.PersistenceError,

            _ => MediaErrors.PersistenceError
        };
    }
}