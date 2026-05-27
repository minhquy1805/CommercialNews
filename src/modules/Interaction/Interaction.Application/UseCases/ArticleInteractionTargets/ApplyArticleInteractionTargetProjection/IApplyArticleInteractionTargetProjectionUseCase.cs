using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace Interaction.Application.UseCases.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

public interface IApplyArticleInteractionTargetProjectionUseCase
{
    Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>> ExecuteAsync(
        ApplyArticleInteractionTargetProjectionRequestDto request,
        CancellationToken cancellationToken = default);
}