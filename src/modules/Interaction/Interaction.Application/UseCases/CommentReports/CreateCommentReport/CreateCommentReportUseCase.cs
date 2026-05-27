using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentReports.CreateCommentReport;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.CommentReports;
using Interaction.Domain.Constants;

namespace Interaction.Application.UseCases.CommentReports.CreateCommentReport;

public sealed class CreateCommentReportUseCase : ICreateCommentReportUseCase
{
    private const string ReportThresholdReachedAlertReason =
        "ReportThresholdReached";

    private readonly ICommentReportRepository _commentReportRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly ICommentReportPolicy _commentReportPolicy;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public CreateCommentReportUseCase(
        ICommentReportRepository commentReportRepository,
        IInteractionUnitOfWork unitOfWork,
        IInteractionOutboxWriter outboxWriter,
        ICommentReportPolicy commentReportPolicy,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator)
    {
        _commentReportRepository = commentReportRepository
            ?? throw new ArgumentNullException(nameof(commentReportRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));

        _commentReportPolicy = commentReportPolicy
            ?? throw new ArgumentNullException(nameof(commentReportPolicy));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<Result<CreateCommentReportResponseDto>> ExecuteAsync(
        CreateCommentReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = CreateCommentReportValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<CreateCommentReportResponseDto>.Failure(
                validationError);
        }

        if (!_requestContext.CurrentUserId.HasValue ||
            _requestContext.CurrentUserId.Value <= 0)
        {
            return Result<CreateCommentReportResponseDto>.Failure(
                InteractionErrors.CommentReport.AuthenticationRequired);
        }

        var commentPublicId =
            CreateCommentReportValidator.NormalizeCommentPublicId(
                request.CommentPublicId);

        var reasonCode =
            CreateCommentReportValidator.NormalizeReasonCode(
                request.ReasonCode);

        var description =
            CreateCommentReportValidator.NormalizeDescription(
                request.Description);

        var reporterUserId = _requestContext.CurrentUserId.Value;

        /*
         * Evaluate policy before opening the SQL transaction.
         * The policy may later read configuration or external rule sources.
         */
        var policyResult = await _commentReportPolicy.EvaluateAsync(
            reasonCode,
            description,
            cancellationToken);

        if (!ReportSeverities.IsValid(policyResult.EvaluatedSeverity) ||
            policyResult.NormalAlertThreshold < 1)
        {
            return Result<CreateCommentReportResponseDto>.Failure(
                InteractionErrors.CommentReport.CreationFailed);
        }

        var reportPublicId = _publicIdGenerator.NewId();
        var newCasePublicId = _publicIdGenerator.NewId();

        /*
         * The procedure stores this candidate only if alert threshold is crossed.
         * If no alert is triggered, it is intentionally unused.
         */
        var alertMessageIdCandidate = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var mutationResult = await _commentReportRepository.CreateAsync(
                reportPublicId: reportPublicId,
                newCasePublicId: newCasePublicId,
                commentPublicId: commentPublicId,
                reporterUserId: reporterUserId,
                reasonCode: reasonCode,
                description: description,
                evaluatedSeverity: policyResult.EvaluatedSeverity,
                normalAlertThreshold: policyResult.NormalAlertThreshold,
                alertMessageIdCandidate: alertMessageIdCandidate,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    mutationResult.ReportStatus,
                    CommentReportStatuses.Pending,
                    StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(mutationResult.CommentReportPublicId) ||
                string.IsNullOrWhiteSpace(mutationResult.CommentModerationCasePublicId) ||
                mutationResult.CreatedAtUtc == default)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<CreateCommentReportResponseDto>.Failure(
                    InteractionErrors.CommentReport.CreationFailed);
            }

            var reportMessageId = _publicIdGenerator.NewId();

            var reportPayload = new CommentReportedPayload(
                CommentReportPublicId: mutationResult.CommentReportPublicId,
                CommentPublicId: mutationResult.CommentPublicId,
                CommentModerationCasePublicId: mutationResult.CommentModerationCasePublicId,
                ReporterUserId: mutationResult.ReporterUserId,
                ReasonCode: mutationResult.ReasonCode,
                ReportStatus: mutationResult.ReportStatus,
                CreatedNewCase: mutationResult.CreatedNewCase,
                CreatedAtUtc: mutationResult.CreatedAtUtc);

            await _outboxWriter.WriteCommentReportedAsync(
                messageId: reportMessageId,
                aggregatePublicId: mutationResult.CommentReportPublicId,
                aggregateVersion: 1,
                payload: reportPayload,
                correlationId: _requestContext.CorrelationId,
                initiatorUserId: reporterUserId,
                occurredAtUtc: mutationResult.CreatedAtUtc,
                cancellationToken: cancellationToken);

            if (mutationResult.AlertTriggered)
            {
                if (string.IsNullOrWhiteSpace(mutationResult.AlertLevel) ||
                    !mutationResult.AlertTriggeredAtUtc.HasValue ||
                    mutationResult.DistinctReporterCount < 1 ||
                    mutationResult.CaseVersion < 1)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<CreateCommentReportResponseDto>.Failure(
                        InteractionErrors.CommentReport.CreationFailed);
                }

                var alertPayload = new CommentReportAlertTriggeredPayload(
                    CommentModerationCasePublicId: mutationResult.CommentModerationCasePublicId,
                    CommentPublicId: mutationResult.CommentPublicId,
                    ArticlePublicId: mutationResult.ArticlePublicId,
                    AlertLevel: mutationResult.AlertLevel,
                    AlertReason: ReportThresholdReachedAlertReason,
                    DistinctReporterCount: mutationResult.DistinctReporterCount,
                    HighestSeverity: mutationResult.HighestSeverity,
                    TriggeredAtUtc: mutationResult.AlertTriggeredAtUtc.Value);

                await _outboxWriter.WriteCommentReportAlertTriggeredAsync(
                    messageId: alertMessageIdCandidate,
                    aggregatePublicId: mutationResult.CommentModerationCasePublicId,
                    aggregateVersion: mutationResult.CaseVersion,
                    payload: alertPayload,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: reporterUserId,
                    occurredAtUtc: mutationResult.AlertTriggeredAtUtc.Value,
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<CreateCommentReportResponseDto>.Success(
                new CreateCommentReportResponseDto
                {
                    CommentReportPublicId = mutationResult.CommentReportPublicId,
                    CommentPublicId = mutationResult.CommentPublicId,
                    Status = mutationResult.ReportStatus,
                    CreatedAtUtc = mutationResult.CreatedAtUtc
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