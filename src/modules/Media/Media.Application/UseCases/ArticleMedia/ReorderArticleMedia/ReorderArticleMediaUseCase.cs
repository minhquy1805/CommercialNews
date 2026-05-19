using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Models.Commands;
using Media.Application.Models.Results;
using Media.Application.Outbox.Payloads;
using Media.Application.Ports.Persistence;
using Media.Application.Ports.Services;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.ReorderArticleMedia;

public sealed class ReorderArticleMediaUseCase : IReorderArticleMediaUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaOutboxWriter _mediaOutboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public ReorderArticleMediaUseCase(
        IArticleMediaRepository articleMediaRepository,
        IMediaUnitOfWork unitOfWork,
        IMediaOutboxWriter mediaOutboxWriter,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _articleMediaRepository = articleMediaRepository
            ?? throw new ArgumentNullException(nameof(articleMediaRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _mediaOutboxWriter = mediaOutboxWriter
            ?? throw new ArgumentNullException(nameof(mediaOutboxWriter));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<ReorderArticleMediaResponse>> ExecuteAsync(
        ReorderArticleMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.ExpectedVersion is null)
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.ExpectedVersionRequired);
            }

            if (request.Items.Count == 0)
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidReorderList);
            }

            if (request.Items.Any(item => item.MediaId <= 0))
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidMediaId);
            }

            if (request.Items.Any(item => item.SortOrder < 0))
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidSortOrder);
            }

            if (request.Items.Select(item => item.MediaId).Distinct().Count() != request.Items.Count)
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidReorderList);
            }

            if (request.Items.Select(item => item.SortOrder).Distinct().Count() != request.Items.Count)
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidReorderList);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            if (actorUserId is null or <= 0)
            {
                return Result<ReorderArticleMediaResponse>.Failure(
                    MediaErrors.Actor.NotFound);
            }

            IReadOnlyList<ArticleMediaOrderItem> orderItems = request.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => new ArticleMediaOrderItem(
                    MediaId: item.MediaId,
                    SortOrder: item.SortOrder))
                .ToArray();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                ArticleMediaMutationResult reorderResult =
                    await _articleMediaRepository.ReorderByIdsAsync(
                        new ReorderArticleMediaCommand(
                            ArticleId: request.ArticleId,
                            ExpectedVersion: request.ExpectedVersion,
                            Orders: orderItems,
                            UpdatedBy: actorUserId),
                        cancellationToken);

                if (!reorderResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<ReorderArticleMediaResponse>.Failure(
                        MapMutationResultCode(reorderResult.ResultCode));
                }

                int attachmentSetVersion = reorderResult.NewVersion
                    ?? throw new InvalidOperationException(
                        "Media_ArticleMedia_ReorderByIds did not return NewVersion.");

                if (reorderResult.AffectedRows > 0)
                {
                    ArticleMediaReorderedItem[] eventItems = orderItems
                        .Select(item => new ArticleMediaReorderedItem(
                            MediaId: item.MediaId,
                            SortOrder: item.SortOrder))
                        .ToArray();

                    await _mediaOutboxWriter.EnqueueArticleMediaReorderedAsync(
                        unitOfWork: _unitOfWork,
                        articleId: request.ArticleId,
                        items: eventItems,
                        actorUserId: actorUserId.Value,
                        attachmentSetVersion: attachmentSetVersion,
                        reorderedAtUtc: nowUtc,
                        correlationId: _requestContext.CorrelationId,
                        cancellationToken: cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ReorderArticleMediaResponse>.Success(
                    new ReorderArticleMediaResponse
                    {
                        ArticleId = request.ArticleId,
                        Reordered = reorderResult.AffectedRows > 0,
                        AffectedRows = reorderResult.AffectedRows,
                        AttachmentSetVersion = attachmentSetVersion
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
            return Result<ReorderArticleMediaResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<ReorderArticleMediaResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapMutationResultCode(int resultCode)
    {
        return resultCode switch
        {
            1 => MediaErrors.ArticleMedia.NotFound,
            3 => MediaErrors.VersionConflict,
            4 => MediaErrors.ArticleMedia.InvalidReorderList,
            6 => MediaErrors.ExpectedVersionRequired,
            _ => MediaErrors.PersistenceError
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" =>
                MediaErrors.ArticleMedia.InvalidArticleId,

            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" =>
                MediaErrors.ArticleMedia.InvalidMediaId,

            "MEDIA.ARTICLE_MEDIA_SORT_ORDER_INVALID" =>
                MediaErrors.ArticleMedia.InvalidSortOrder,

            "MEDIA.INVALID_REORDER_LIST" =>
                MediaErrors.ArticleMedia.InvalidReorderList,

            "MEDIA.EXPECTED_VERSION_REQUIRED" =>
                MediaErrors.ExpectedVersionRequired,

            "MEDIA.VERSION_CONFLICT" =>
                MediaErrors.VersionConflict,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_NOT_FOUND" =>
                MediaErrors.Article.NotFound,

            "MEDIA.MEDIA_NOT_FOUND" =>
                MediaErrors.MediaAsset.NotFound,

            "MEDIA.ATTACHMENT_NOT_FOUND" =>
                MediaErrors.ArticleMedia.NotFound,

            "MEDIA.ACTOR_NOT_FOUND" =>
                MediaErrors.Actor.NotFound,

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