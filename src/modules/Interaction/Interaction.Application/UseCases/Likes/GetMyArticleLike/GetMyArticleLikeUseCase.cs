using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Likes.GetMyArticleLike;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.Likes;

namespace Interaction.Application.UseCases.Likes.GetMyArticleLike;

public sealed class GetMyArticleLikeUseCase : IGetMyArticleLikeUseCase
{
    private readonly IArticleLikeRepository _articleLikeRepository;
    private readonly IRequestContext _requestContext;

    public GetMyArticleLikeUseCase(
        IArticleLikeRepository articleLikeRepository,
        IRequestContext requestContext)
    {
        _articleLikeRepository = articleLikeRepository
            ?? throw new ArgumentNullException(nameof(articleLikeRepository));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<GetMyArticleLikeResponseDto>> ExecuteAsync(
        GetMyArticleLikeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = GetMyArticleLikeValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<GetMyArticleLikeResponseDto>.Failure(validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<GetMyArticleLikeResponseDto>.Failure(
                InteractionErrors.Like.AuthenticationRequired);
        }

        var articlePublicId =
            GetMyArticleLikeValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var currentUserId = _requestContext.CurrentUserId.Value;

        var articleLike =
            await _articleLikeRepository.GetByArticlePublicIdAndUserIdAsync(
                articlePublicId,
                currentUserId,
                cancellationToken);

        if (articleLike is null)
        {
            return Result<GetMyArticleLikeResponseDto>.Success(
                new GetMyArticleLikeResponseDto
                {
                    ArticlePublicId = articlePublicId,
                    Liked = false,
                    Version = null
                });
        }

        return Result<GetMyArticleLikeResponseDto>.Success(
            new GetMyArticleLikeResponseDto
            {
                ArticlePublicId = articleLike.ArticlePublicId,
                Liked = articleLike.IsActive,
                Version = articleLike.Version
            });
    }
}