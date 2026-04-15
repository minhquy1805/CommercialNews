using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.ReorderArticleMedia;

public sealed class ReorderArticleMediaUseCase : IReorderArticleMediaUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;

    public ReorderArticleMediaUseCase(
        IArticleMediaRepository articleMediaRepository,
        IMediaUnitOfWork unitOfWork,
        IRequestContext requestContext)
    {
        _articleMediaRepository = articleMediaRepository;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public async Task<Result<ReorderArticleMediaResponse>> ExecuteAsync(
        ReorderArticleMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<ReorderArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                return Result<ReorderArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidReorderList);
            }

            bool hasInvalidMediaId = request.Items.Any(item => item.MediaId <= 0);
            if (hasInvalidMediaId)
            {
                return Result<ReorderArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidMediaId);
            }

            bool hasInvalidSortOrder = request.Items.Any(item => item.SortOrder < 0);
            if (hasInvalidSortOrder)
            {
                return Result<ReorderArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidSortOrder);
            }

            bool hasDuplicateMediaIds = request.Items
                .GroupBy(item => item.MediaId)
                .Any(group => group.Count() > 1);

            if (hasDuplicateMediaIds)
            {
                return Result<ReorderArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidReorderList);
            }

            bool hasDuplicateSortOrders = request.Items
                .GroupBy(item => item.SortOrder)
                .Any(group => group.Count() > 1);

            if (hasDuplicateSortOrders)
            {
                return Result<ReorderArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidReorderList);
            }

            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            IReadOnlyList<(long MediaId, int SortOrder)> orders = request.Items
                .Select(item => (item.MediaId, item.SortOrder))
                .ToArray();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _articleMediaRepository.ReorderByIdsAsync(
                    request.ArticleId,
                    orders,
                    actorUserId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ReorderArticleMediaResponse>.Success(new ReorderArticleMediaResponse
                {
                    ArticleId = request.ArticleId,
                    Reordered = affectedRows > 0,
                    AffectedRows = affectedRows
                });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<ReorderArticleMediaResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<ReorderArticleMediaResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" => MediaErrors.ArticleMedia.InvalidArticleId,
            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" => MediaErrors.ArticleMedia.InvalidMediaId,
            "MEDIA.ARTICLE_MEDIA_SORT_ORDER_INVALID" => MediaErrors.ArticleMedia.InvalidSortOrder,
            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.INVALID_REORDER_LIST" => MediaErrors.ArticleMedia.InvalidReorderList,
            _ => MediaErrors.ValidationFailed
        };
    }
}