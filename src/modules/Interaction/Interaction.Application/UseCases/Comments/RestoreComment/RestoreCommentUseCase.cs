using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.RestoreComment;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.Comments;
using Interaction.Domain.Constants;

namespace Interaction.Application.UseCases.Comments.RestoreComment;

public sealed class RestoreCommentUseCase : IRestoreCommentUseCase
{
    private const string ModeratorActorType = "Moderator";

    private readonly ICommentRepository _commentRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public RestoreCommentUseCase(
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

    public async Task<Result<RestoreCommentResponseDto>> ExecuteAsync(
        RestoreCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = RestoreCommentValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<RestoreCommentResponseDto>.Failure(validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<RestoreCommentResponseDto>.Failure(
                InteractionErrors.Comment.AuthenticationRequired);
        }

        var commentPublicId =
            RestoreCommentValidator.NormalizeCommentPublicId(
                request.CommentPublicId);

        var note =
            RestoreCommentValidator.NormalizeNote(
                request.Note);

        var actorUserId = _requestContext.CurrentUserId.Value;
        var historyPublicId = _publicIdGenerator.NewId();
        var messageId = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var moderationResult = await _commentRepository.RestoreAsync(
                commentPublicId: commentPublicId,
                expectedVersion: request.ExpectedVersion,
                historyPublicId: historyPublicId,
                actorUserId: actorUserId,
                note: note,
                correlationId: _requestContext.CorrelationId,
                actorType: ModeratorActorType,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    moderationResult.Status,
                    CommentStatuses.Visible,
                    StringComparison.OrdinalIgnoreCase) ||
                moderationResult.Version < 1 ||
                moderationResult.UpdatedAtUtc == default)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<RestoreCommentResponseDto>.Failure(
                    InteractionErrors.UnexpectedFailure);
            }

            var payload = new CommentRestoredPayload(
                CommentPublicId: moderationResult.CommentPublicId,
                ArticlePublicId: moderationResult.ArticlePublicId,
                ModeratorUserId: actorUserId,
                RestoredAtUtc: moderationResult.UpdatedAtUtc);

            await _outboxWriter.WriteCommentRestoredAsync(
                messageId: messageId,
                aggregatePublicId: moderationResult.CommentPublicId,
                aggregateVersion: checked((int)moderationResult.Version),
                payload: payload,
                correlationId: _requestContext.CorrelationId,
                initiatorUserId: actorUserId,
                occurredAtUtc: moderationResult.UpdatedAtUtc,
                cancellationToken: cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<RestoreCommentResponseDto>.Success(
                new RestoreCommentResponseDto
                {
                    CommentPublicId = moderationResult.CommentPublicId,
                    Status = moderationResult.Status,
                    Version = moderationResult.Version
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
