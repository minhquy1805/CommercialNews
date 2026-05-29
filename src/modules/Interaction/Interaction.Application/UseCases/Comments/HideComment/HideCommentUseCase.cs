using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.HideComment;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.Comments;
using Interaction.Domain.Constants;

namespace Interaction.Application.UseCases.Comments.HideComment;

public sealed class HideCommentUseCase : IHideCommentUseCase
{
    private const string ModeratorActorType = "Moderator";
    private const string DirectResolutionSource = "Direct";

    private readonly ICommentRepository _commentRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public HideCommentUseCase(
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

    public async Task<Result<HideCommentResponseDto>> ExecuteAsync(
        HideCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = HideCommentValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<HideCommentResponseDto>.Failure(validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<HideCommentResponseDto>.Failure(
                InteractionErrors.Comment.AuthenticationRequired);
        }

        var commentPublicId =
            HideCommentValidator.NormalizeCommentPublicId(
                request.CommentPublicId);

        var reasonCode =
            HideCommentValidator.NormalizeReasonCode(
                request.ReasonCode);

        var note =
            HideCommentValidator.NormalizeNote(
                request.Note);

        var actorUserId = _requestContext.CurrentUserId.Value;
        var historyPublicId = _publicIdGenerator.NewId();
        var messageId = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var moderationResult = await _commentRepository.HideAsync(
                commentPublicId: commentPublicId,
                expectedVersion: request.ExpectedVersion,
                historyPublicId: historyPublicId,
                actorUserId: actorUserId,
                reasonCode: reasonCode,
                note: note,
                correlationId: _requestContext.CorrelationId,
                actorType: ModeratorActorType,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    moderationResult.Status,
                    CommentStatuses.Hidden,
                    StringComparison.OrdinalIgnoreCase) ||
                moderationResult.Version < 1 ||
                moderationResult.UpdatedAtUtc == default)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<HideCommentResponseDto>.Failure(
                    InteractionErrors.UnexpectedFailure);
            }

            var payload = new CommentHiddenPayload(
                CommentPublicId: moderationResult.CommentPublicId,
                ArticlePublicId: moderationResult.ArticlePublicId,
                ResolutionSource: DirectResolutionSource,
                CommentModerationCasePublicId: null,
                ResolvedReportCount: null,
                ReasonCode: reasonCode,
                ModeratorUserId: actorUserId,
                HiddenAtUtc: moderationResult.UpdatedAtUtc);

            await _outboxWriter.WriteCommentHiddenAsync(
                messageId: messageId,
                aggregatePublicId: moderationResult.CommentPublicId,
                aggregateVersion: checked((int)moderationResult.Version),
                payload: payload,
                correlationId: _requestContext.CorrelationId,
                initiatorUserId: actorUserId,
                occurredAtUtc: moderationResult.UpdatedAtUtc,
                cancellationToken: cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<HideCommentResponseDto>.Success(
                new HideCommentResponseDto
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
