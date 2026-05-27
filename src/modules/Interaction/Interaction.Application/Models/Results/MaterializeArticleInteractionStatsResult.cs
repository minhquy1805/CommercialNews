using Interaction.Domain.Entities;

namespace Interaction.Application.Models.Results;

public sealed record MaterializeArticleInteractionStatsResult(
    ArticleInteractionStats Stats,
    bool SnapshotChanged);