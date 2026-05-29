using Interaction.Domain.Entities;

namespace Interaction.Application.Models.Results;

public sealed record ApplyArticleInteractionTargetProjectionResult(
    ArticleInteractionTargetProjection? Projection,
    string ApplyDecision);