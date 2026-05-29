using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Interaction.Application.Contracts.Likes.LikeArticle;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.Likes;

namespace Interaction.Application.UseCases.Likes.LikeArticle;

public sealed class LikeArticleUseCase : ILikeArticleUseCase
{
    private readonly IArticleLikeRepository _articleLikeRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LikeArticleUseCase(
        IArticleLikeRepository articleLikeRepository,
        IInteractionUnitOfWork unitOfWork,
        IInteractionOutboxWriter outboxWriter,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider)
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

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<LikeArticleResponseDto>> ExecuteAsync(
        LikeArticleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = LikeArticleValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<LikeArticleResponseDto>.Failure(validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<LikeArticleResponseDto>.Failure(
                InteractionErrors.Like.AuthenticationRequired);
        }

        var articlePublicId =
            LikeArticleValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var currentUserId = _requestContext.CurrentUserId.Value;

        /*
        * Candidate public id for a new ArticleLike row.
        * When the relationship already exists, the stored procedure
        * reuses the existing row and this candidate is not persisted.
        */
        var articleLikePublicIdCandidate = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var mutationResult = await _articleLikeRepository.SetLikedAsync(
                publicId: articleLikePublicIdCandidate,
                articlePublicId: articlePublicId,
                userId: currentUserId,
                cancellationToken: cancellationToken);

            var articleLike = mutationResult.ArticleLike;

            if (articleLike is null || !articleLike.IsActive)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<LikeArticleResponseDto>.Failure(
                    InteractionErrors.Like.StateUnavailable);
            }

            if (mutationResult.Changed)
            {
                var messageId = _publicIdGenerator.NewId();

                var payload = new ArticleLikedPayload(
                    ArticleLikePublicId: articleLike.PublicId,
                    ArticlePublicId: articleLike.ArticlePublicId,
                    UserId: articleLike.UserId,
                    LikedAtUtc: articleLike.LikedAtUtc);

                await _outboxWriter.WriteArticleLikedAsync(
                    messageId: messageId,
                    aggregatePublicId: articleLike.PublicId,
                    aggregateVersion: checked((int)articleLike.Version),
                    payload: payload,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: currentUserId,
                    occurredAtUtc: articleLike.LikedAtUtc,
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<LikeArticleResponseDto>.Success(
                new LikeArticleResponseDto
                {
                    ArticlePublicId = articleLike.ArticlePublicId,
                    Liked = true,
                    Version = articleLike.Version
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
