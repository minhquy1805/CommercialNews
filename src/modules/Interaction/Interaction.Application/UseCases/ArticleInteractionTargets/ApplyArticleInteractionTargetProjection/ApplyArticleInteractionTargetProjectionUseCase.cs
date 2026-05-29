using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.ArticleInteractionTargets;
using Interaction.Domain.Constants;
using Interaction.Domain.Entities;

namespace Interaction.Application.UseCases.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

public sealed class ApplyArticleInteractionTargetProjectionUseCase
    : IApplyArticleInteractionTargetProjectionUseCase
{
    private readonly IArticleInteractionTargetProjectionRepository _repository;

    public ApplyArticleInteractionTargetProjectionUseCase(
        IArticleInteractionTargetProjectionRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>> ExecuteAsync(
        ApplyArticleInteractionTargetProjectionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            ApplyArticleInteractionTargetProjectionValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<ApplyArticleInteractionTargetProjectionResponseDto>.Failure(
                validationError);
        }

        var articlePublicId =
            ApplyArticleInteractionTargetProjectionValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var sourceStatus =
            ApplyArticleInteractionTargetProjectionValidator.NormalizeSourceStatus(
                request.SourceStatus);

        var sourceMessageId =
            ApplyArticleInteractionTargetProjectionValidator.NormalizeSourceMessageId(
                request.SourceMessageId);

        var applyResult = await _repository.ApplyAsync(
            articlePublicId: articlePublicId,
            sourceStatus: sourceStatus,
            isInteractionEnabled: request.IsInteractionEnabled,
            sourceVersion: request.SourceVersion,
            sourceMessageId: sourceMessageId,
            sourceOccurredAtUtc: request.SourceOccurredAtUtc,
            cancellationToken: cancellationToken);

        if (IsDecision(
                applyResult.ApplyDecision,
                ArticleInteractionTargetProjectionApplyDecisions.Applied))
        {
            if (applyResult.Projection is null)
            {
                return Result<ApplyArticleInteractionTargetProjectionResponseDto>.Failure(
                    InteractionErrors.ArticleInteractionTargetProjection.ApplyFailed);
            }

            return Result<ApplyArticleInteractionTargetProjectionResponseDto>.Success(
                MapResponse(
                    applyResult.Projection,
                    ArticleInteractionTargetProjectionApplyDecisions.Applied));
        }

        if (IsDecision(
                applyResult.ApplyDecision,
                ArticleInteractionTargetProjectionApplyDecisions.StaleIgnored))
        {
            var currentProjection = applyResult.Projection
                ?? await _repository.GetByArticlePublicIdAsync(
                    articlePublicId,
                    cancellationToken);

            if (currentProjection is null)
            {
                return Result<ApplyArticleInteractionTargetProjectionResponseDto>.Failure(
                    InteractionErrors.ArticleInteractionTargetProjection.ApplyFailed);
            }

            return Result<ApplyArticleInteractionTargetProjectionResponseDto>.Success(
                MapResponse(
                    currentProjection,
                    ArticleInteractionTargetProjectionApplyDecisions.StaleIgnored));
        }

        if (IsDecision(
                applyResult.ApplyDecision,
                ArticleInteractionTargetProjectionApplyDecisions.ResyncRequired))
        {
            return Result<ApplyArticleInteractionTargetProjectionResponseDto>.Failure(
                InteractionErrors.ArticleInteractionTargetProjection.RequiresResync);
        }

        return Result<ApplyArticleInteractionTargetProjectionResponseDto>.Failure(
            InteractionErrors.ArticleInteractionTargetProjection.ApplyFailed);
    }

    private static ApplyArticleInteractionTargetProjectionResponseDto MapResponse(
        ArticleInteractionTargetProjection projection,
        string applyDecision)
    {
        return new ApplyArticleInteractionTargetProjectionResponseDto
        {
            ArticlePublicId = projection.ArticlePublicId,
            ApplyDecision = applyDecision,
            SourceStatus = projection.SourceStatus,
            IsInteractionEnabled = projection.IsInteractionEnabled,
            LastSourceVersion = projection.LastSourceVersion
        };
    }

    private static bool IsDecision(
        string actualDecision,
        string expectedDecision)
    {
        return string.Equals(
            actualDecision,
            expectedDecision,
            StringComparison.OrdinalIgnoreCase);
    }
}