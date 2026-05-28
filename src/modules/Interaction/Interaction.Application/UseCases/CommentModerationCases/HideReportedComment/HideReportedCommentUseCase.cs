using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.HideReportedComment;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.CommentModerationCases;
using Interaction.Domain.Constants;

namespace Interaction.Application.UseCases.CommentModerationCases.HideReportedComment;

public sealed class HideReportedCommentUseCase
    : IHideReportedCommentUseCase
{
    private const string ModeratorActorType = "Moderator";
    private const string ReportResolutionSource = "Report";

    private readonly ICommentModerationCaseRepository _moderationCaseRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public HideReportedCommentUseCase(
        ICommentModerationCaseRepository moderationCaseRepository,
        IInteractionUnitOfWork unitOfWork,
        IInteractionOutboxWriter outboxWriter,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator)
    {
        _moderationCaseRepository = moderationCaseRepository
            ?? throw new ArgumentNullException(nameof(moderationCaseRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<Result<HideReportedCommentResponseDto>> ExecuteAsync(
        HideReportedCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = HideReportedCommentValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<HideReportedCommentResponseDto>.Failure(
                validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<HideReportedCommentResponseDto>.Failure(
                InteractionErrors.Comment.AuthenticationRequired);
        }

        var casePublicId =
            HideReportedCommentValidator.NormalizeCasePublicId(
                request.CasePublicId);

        var reasonCode =
            HideReportedCommentValidator.NormalizeReasonCode(
                request.ReasonCode);

        var note =
            HideReportedCommentValidator.NormalizeNote(
                request.Note);

        var moderatorUserId = _requestContext.CurrentUserId.Value;
        var historyPublicId = _publicIdGenerator.NewId();
        var messageId = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var hideResult = await _moderationCaseRepository.HideCommentAsync(
                casePublicId: casePublicId,
                expectedCaseVersion: request.ExpectedCaseVersion,
                expectedCommentVersion: request.ExpectedCommentVersion,
                historyPublicId: historyPublicId,
                actorUserId: moderatorUserId,
                reasonCode: reasonCode,
                note: note,
                correlationId: _requestContext.CorrelationId,
                actorType: ModeratorActorType,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    hideResult.CaseStatus,
                    CommentModerationCaseStatuses.Actioned,
                    StringComparison.OrdinalIgnoreCase) ||
                hideResult.CaseVersion < 1 ||
                !hideResult.ResolvedAtUtc.HasValue ||
                !string.Equals(
                    hideResult.CommentStatus,
                    CommentStatuses.Hidden,
                    StringComparison.OrdinalIgnoreCase) ||
                hideResult.CommentVersion < 1 ||
                !hideResult.HiddenAtUtc.HasValue ||
                hideResult.ResolvedReportCount < 1)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<HideReportedCommentResponseDto>.Failure(
                    InteractionErrors.UnexpectedFailure);
            }

            var resolvedAtUtc = hideResult.ResolvedAtUtc.Value;
            var hiddenAtUtc = hideResult.HiddenAtUtc.Value;

            var payload = new CommentHiddenPayload(
                CommentPublicId: hideResult.CommentPublicId,
                ArticlePublicId: hideResult.ArticlePublicId,
                ResolutionSource: ReportResolutionSource,
                CommentModerationCasePublicId:
                    hideResult.CommentModerationCasePublicId,
                ResolvedReportCount: hideResult.ResolvedReportCount,
                ReasonCode: reasonCode,
                ModeratorUserId: moderatorUserId,
                HiddenAtUtc: hiddenAtUtc);

            /*
             * The aggregate of interaction.comment_hidden is Comment,
             * not CommentModerationCase, because the externally relevant
             * business state transition is Comment Visible -> Hidden.
             */
            await _outboxWriter.WriteCommentHiddenAsync(
                messageId: messageId,
                aggregatePublicId: hideResult.CommentPublicId,
                aggregateVersion: checked((int)hideResult.CommentVersion),
                payload: payload,
                correlationId: _requestContext.CorrelationId,
                initiatorUserId: moderatorUserId,
                occurredAtUtc: hiddenAtUtc,
                cancellationToken: cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<HideReportedCommentResponseDto>.Success(
                new HideReportedCommentResponseDto
                {
                    CommentModerationCasePublicId =
                        hideResult.CommentModerationCasePublicId,
                    CaseStatus = hideResult.CaseStatus,
                    CaseVersion = hideResult.CaseVersion,
                    CommentPublicId = hideResult.CommentPublicId,
                    CommentStatus = hideResult.CommentStatus,
                    CommentVersion = hideResult.CommentVersion,
                    ResolvedAtUtc = resolvedAtUtc,
                    HiddenAtUtc = hiddenAtUtc
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
