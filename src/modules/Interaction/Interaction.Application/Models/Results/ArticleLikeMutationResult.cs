using Interaction.Domain.Entities;

namespace Interaction.Application.Models.Results;

public sealed record ArticleLikeMutationResult(
    ArticleLike? ArticleLike,
    bool Changed);