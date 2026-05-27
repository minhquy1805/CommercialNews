using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.CreateComment;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.Comments;

namespace Interaction.Application.UseCases.Comments.CreateComment;

public sealed class CreateCommentUseCase : ICreateCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly ICommentContentPolicy _commentContentPolicy;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public CreateCommentUseCase(
        ICommentRepository commentRepository,
        IInteractionUnitOfWork unitOfWork,
        IInteractionOutboxWriter outboxWriter,
        ICommentContentPolicy commentContentPolicy,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator)
    {
        _commentRepository = commentRepository
            ?? throw new ArgumentNullException(nameof(commentRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));

        _commentContentPolicy = commentContentPolicy
            ?? throw new ArgumentNullException(nameof(commentContentPolicy));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<Result<CreateCommentResponseDto>> ExecuteAsync(
        CreateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = CreateCommentValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<CreateCommentResponseDto>.Failure(validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<CreateCommentResponseDto>.Failure(
                InteractionErrors.Comment.AuthenticationRequired);
        }

        var articlePublicId =
            CreateCommentValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var content =
            CreateCommentValidator.NormalizeContent(
                request.Content);

        var policyResult = await _commentContentPolicy.EvaluateAsync(
            content,
            cancellationToken);

        if (!policyResult.IsAllowed)
        {
            return Result<CreateCommentResponseDto>.Failure(
                InteractionErrors.Comment.ProhibitedContent);
        }

        var currentUserId = _requestContext.CurrentUserId.Value;
        var commentPublicId = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var comment = await _commentRepository.InsertVisibleAsync(
                publicId: commentPublicId,
                articlePublicId: articlePublicId,
                authorUserId: currentUserId,
                content: content,
                cancellationToken: cancellationToken);

            /*
             * The InsertVisible procedure contract must return a freshly-created
             * visible comment. This guard prevents publishing an invalid event
             * if persistence mapping or SQL behavior ever drifts.
             */
            if (!comment.IsVisible() ||
                comment.Version < 1)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<CreateCommentResponseDto>.Failure(
                    InteractionErrors.UnexpectedFailure);
            }

            var messageId = _publicIdGenerator.NewId();

            var payload = new CommentCreatedPayload(
                CommentPublicId: comment.PublicId,
                ArticlePublicId: comment.ArticlePublicId,
                AuthorUserId: comment.AuthorUserId,
                Status: comment.Status,
                CreatedAtUtc: comment.CreatedAtUtc);

            await _outboxWriter.WriteCommentCreatedAsync(
                messageId: messageId,
                aggregatePublicId: comment.PublicId,
                aggregateVersion: comment.Version,
                payload: payload,
                correlationId: _requestContext.CorrelationId,
                initiatorUserId: currentUserId,
                occurredAtUtc: comment.CreatedAtUtc,
                cancellationToken: cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<CreateCommentResponseDto>.Success(
                new CreateCommentResponseDto
                {
                    CommentPublicId = comment.PublicId,
                    ArticlePublicId = comment.ArticlePublicId,
                    Status = comment.Status,
                    CreatedAtUtc = comment.CreatedAtUtc,
                    Version = comment.Version
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