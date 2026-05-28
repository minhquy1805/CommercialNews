using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Likes.UnlikeArticle;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.Likes;

namespace Interaction.Application.UseCases.Likes.UnlikeArticle;

public sealed class UnlikeArticleUseCase : IUnlikeArticleUseCase
{
    private readonly IArticleLikeRepository _articleLikeRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public UnlikeArticleUseCase(
        IArticleLikeRepository articleLikeRepository,
        IInteractionUnitOfWork unitOfWork,
        IInteractionOutboxWriter outboxWriter,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator)
    {
        _articleLikeRepository = articleLikeRepository
            ?? throw new ArgumentNullException(nameof(articleLikeRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<Result<UnlikeArticleResponseDto>> ExecuteAsync(
        UnlikeArticleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = UnlikeArticleValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<UnlikeArticleResponseDto>.Failure(validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<UnlikeArticleResponseDto>.Failure(
                InteractionErrors.Like.AuthenticationRequired);
        }

        var articlePublicId =
            UnlikeArticleValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var currentUserId = _requestContext.CurrentUserId.Value;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var mutationResult = await _articleLikeRepository.SetUnlikedAsync(
                articlePublicId: articlePublicId,
                userId: currentUserId,
                cancellationToken: cancellationToken);

            var articleLike = mutationResult.ArticleLike;

            /*
             * Changed = true means an active relationship was actually
             * transitioned to inactive. In that case persistence must return
             * the committed inactive row so the integration event can be built.
             */
            if (mutationResult.Changed)
            {
                if (articleLike is null ||
                    articleLike.IsActive ||
                    !articleLike.UnlikedAtUtc.HasValue)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<UnlikeArticleResponseDto>.Failure(
                        InteractionErrors.Like.StateUnavailable);
                }

                var messageId = _publicIdGenerator.NewId();

                var payload = new ArticleUnlikedPayload(
                    ArticleLikePublicId: articleLike.PublicId,
                    ArticlePublicId: articleLike.ArticlePublicId,
                    UserId: articleLike.UserId,
                    UnlikedAtUtc: articleLike.UnlikedAtUtc.Value);

                await _outboxWriter.WriteArticleUnlikedAsync(
                    messageId: messageId,
                    aggregatePublicId: articleLike.PublicId,
                    aggregateVersion: checked((int)articleLike.Version),
                    payload: payload,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: currentUserId,
                    occurredAtUtc: articleLike.UnlikedAtUtc.Value,
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<UnlikeArticleResponseDto>.Success(
                new UnlikeArticleResponseDto
                {
                    ArticlePublicId = articleLike?.ArticlePublicId ?? articlePublicId,
                    Liked = false,
                    Version = articleLike?.Version
                });
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }
}
