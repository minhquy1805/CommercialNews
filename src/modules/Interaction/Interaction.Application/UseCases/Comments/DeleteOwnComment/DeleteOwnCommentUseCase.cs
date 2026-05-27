using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.DeleteOwnComment;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.Comments;
using Interaction.Domain.Constants;

namespace Interaction.Application.UseCases.Comments.DeleteOwnComment;

public sealed class DeleteOwnCommentUseCase : IDeleteOwnCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public DeleteOwnCommentUseCase(
        ICommentRepository commentRepository,
        IInteractionUnitOfWork unitOfWork,
        IInteractionOutboxWriter outboxWriter,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator)
    {
        _commentRepository = commentRepository
            ?? throw new ArgumentNullException(nameof(commentRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<Result<DeleteOwnCommentResponseDto>> ExecuteAsync(
        DeleteOwnCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = DeleteOwnCommentValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<DeleteOwnCommentResponseDto>.Failure(validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<DeleteOwnCommentResponseDto>.Failure(
                InteractionErrors.Comment.AuthenticationRequired);
        }

        var commentPublicId =
            DeleteOwnCommentValidator.NormalizeCommentPublicId(
                request.CommentPublicId);

        var currentUserId = _requestContext.CurrentUserId.Value;

        /*
         * Candidate public id for local moderation history.
         * The stored procedure uses it only when deleting this comment
         * closes an existing open moderation case.
         */
        var caseCloseHistoryPublicIdCandidate = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var mutationResult = await _commentRepository.DeleteOwnAsync(
                commentPublicId: commentPublicId,
                authorUserId: currentUserId,
                expectedVersion: request.ExpectedVersion,
                caseCloseHistoryPublicId: caseCloseHistoryPublicIdCandidate,
                correlationId: _requestContext.CorrelationId,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    mutationResult.Status,
                    CommentStatuses.Deleted,
                    StringComparison.OrdinalIgnoreCase) ||
                mutationResult.Version < 1 ||
                !mutationResult.DeletedAtUtc.HasValue)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<DeleteOwnCommentResponseDto>.Failure(
                    InteractionErrors.UnexpectedFailure);
            }

            if (mutationResult.Changed)
            {
                var messageId = _publicIdGenerator.NewId();

                var payload = new CommentDeletedByAuthorPayload(
                    CommentPublicId: mutationResult.CommentPublicId,
                    ArticlePublicId: mutationResult.ArticlePublicId,
                    AuthorUserId: mutationResult.AuthorUserId,
                    WasVisible: mutationResult.WasVisible,
                    ClosedOpenCase: mutationResult.ClosedOpenCase,
                    DeletedAtUtc: mutationResult.DeletedAtUtc.Value);

                await _outboxWriter.WriteCommentDeletedByAuthorAsync(
                    messageId: messageId,
                    aggregatePublicId: mutationResult.CommentPublicId,
                    aggregateVersion: mutationResult.Version,
                    payload: payload,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: currentUserId,
                    occurredAtUtc: mutationResult.DeletedAtUtc.Value,
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<DeleteOwnCommentResponseDto>.Success(
                new DeleteOwnCommentResponseDto
                {
                    CommentPublicId = mutationResult.CommentPublicId,
                    ArticlePublicId = mutationResult.ArticlePublicId,
                    Status = mutationResult.Status,
                    DeletedAtUtc = mutationResult.DeletedAtUtc.Value,
                    Version = mutationResult.Version
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