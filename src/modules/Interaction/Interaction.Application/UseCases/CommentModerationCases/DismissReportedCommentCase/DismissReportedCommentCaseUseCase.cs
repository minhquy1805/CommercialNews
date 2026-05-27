using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.DismissReportedCommentCase;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.CommentModerationCases;
using Interaction.Domain.Constants;

namespace Interaction.Application.UseCases.CommentModerationCases.DismissReportedCommentCase;

public sealed class DismissReportedCommentCaseUseCase
    : IDismissReportedCommentCaseUseCase
{
    private const string ModeratorActorType = "Moderator";

    private readonly ICommentModerationCaseRepository _moderationCaseRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public DismissReportedCommentCaseUseCase(
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

    public async Task<Result<DismissReportedCommentCaseResponseDto>> ExecuteAsync(
        DismissReportedCommentCaseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            DismissReportedCommentCaseValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<DismissReportedCommentCaseResponseDto>.Failure(
                validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<DismissReportedCommentCaseResponseDto>.Failure(
                InteractionErrors.Comment.AuthenticationRequired);
        }

        var casePublicId =
            DismissReportedCommentCaseValidator.NormalizeCasePublicId(
                request.CasePublicId);

        var reasonCode =
            DismissReportedCommentCaseValidator.NormalizeReasonCode(
                request.ReasonCode);

        var note =
            DismissReportedCommentCaseValidator.NormalizeNote(
                request.Note);

        var moderatorUserId = _requestContext.CurrentUserId.Value;
        var historyPublicId = _publicIdGenerator.NewId();
        var messageId = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var dismissResult = await _moderationCaseRepository.DismissAsync(
                casePublicId: casePublicId,
                expectedVersion: request.ExpectedCaseVersion,
                historyPublicId: historyPublicId,
                actorUserId: moderatorUserId,
                reasonCode: reasonCode,
                note: note,
                correlationId: _requestContext.CorrelationId,
                actorType: ModeratorActorType,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    dismissResult.CaseStatus,
                    CommentModerationCaseStatuses.Dismissed,
                    StringComparison.OrdinalIgnoreCase) ||
                dismissResult.CaseVersion < 1 ||
                !dismissResult.ResolvedAtUtc.HasValue)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<DismissReportedCommentCaseResponseDto>.Failure(
                    InteractionErrors.UnexpectedFailure);
            }

            var resolvedAtUtc = dismissResult.ResolvedAtUtc.Value;

            var payload = new CommentReportsDismissedPayload(
                CommentModerationCasePublicId:
                    dismissResult.CommentModerationCasePublicId,
                CaseStatus: dismissResult.CaseStatus,
                ReasonCode: reasonCode,
                ModeratorUserId: moderatorUserId,
                ResolvedAtUtc: resolvedAtUtc);

            await _outboxWriter.WriteCommentReportsDismissedAsync(
                messageId: messageId,
                aggregatePublicId: dismissResult.CommentModerationCasePublicId,
                aggregateVersion: dismissResult.CaseVersion,
                payload: payload,
                correlationId: _requestContext.CorrelationId,
                initiatorUserId: moderatorUserId,
                occurredAtUtc: resolvedAtUtc,
                cancellationToken: cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<DismissReportedCommentCaseResponseDto>.Success(
                new DismissReportedCommentCaseResponseDto
                {
                    CommentModerationCasePublicId =
                        dismissResult.CommentModerationCasePublicId,
                    Status = dismissResult.CaseStatus,
                    ResolvedAtUtc = resolvedAtUtc,
                    Version = dismissResult.CaseVersion
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