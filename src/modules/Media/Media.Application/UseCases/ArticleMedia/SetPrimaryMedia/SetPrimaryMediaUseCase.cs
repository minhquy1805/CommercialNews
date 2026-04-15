using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.SetPrimaryMedia;

public sealed class SetPrimaryMediaUseCase : ISetPrimaryMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;

    public SetPrimaryMediaUseCase(
        IMediaAssetRepository mediaAssetRepository,
        IArticleMediaRepository articleMediaRepository,
        IMediaUnitOfWork unitOfWork,
        IRequestContext requestContext)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _articleMediaRepository = articleMediaRepository;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public async Task<Result<SetPrimaryMediaResponse>> ExecuteAsync(
        SetPrimaryMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<SetPrimaryMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.MediaId <= 0)
            {
                return Result<SetPrimaryMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidMediaId);
            }

            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            var mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<SetPrimaryMediaResponse>.Failure(MediaErrors.MediaAsset.NotFound);
            }

            if (mediaAsset.IsDeleted)
            {
                return Result<SetPrimaryMediaResponse>.Failure(MediaErrors.MediaAsset.AlreadyDeleted);
            }

            var existingPrimary = await _articleMediaRepository.GetPrimaryByArticleIdAsync(
                request.ArticleId,
                cancellationToken);

            if (existingPrimary is not null && existingPrimary.MediaId == request.MediaId)
            {
                return Result<SetPrimaryMediaResponse>.Success(new SetPrimaryMediaResponse
                {
                    ArticleId = request.ArticleId,
                    MediaId = request.MediaId,
                    PrimarySet = true,
                    AffectedRows = 0
                });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _articleMediaRepository.SetPrimaryAsync(
                    request.ArticleId,
                    request.MediaId,
                    actorUserId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                if (affectedRows <= 0)
                {
                    return Result<SetPrimaryMediaResponse>.Failure(MediaErrors.ArticleMedia.NotFound);
                }

                return Result<SetPrimaryMediaResponse>.Success(new SetPrimaryMediaResponse
                {
                    ArticleId = request.ArticleId,
                    MediaId = request.MediaId,
                    PrimarySet = affectedRows > 0,
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
            return Result<SetPrimaryMediaResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<SetPrimaryMediaResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" => MediaErrors.ArticleMedia.InvalidArticleId,
            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" => MediaErrors.ArticleMedia.InvalidMediaId,
            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" => MediaErrors.ArticleMedia.PrimaryConstraintViolation,
            "MEDIA.MEDIA_ASSET_ALREADY_DELETED" => MediaErrors.MediaAsset.AlreadyDeleted,
            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" => MediaErrors.ArticleMedia.PrimaryConstraintViolation,
            "MEDIA.ATTACHMENT_NOT_FOUND" => MediaErrors.ArticleMedia.NotFound,
            _ => MediaErrors.ValidationFailed
        };
    }
}