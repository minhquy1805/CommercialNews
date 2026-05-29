using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Interaction.Application.Models.Results;

public sealed record ArticleViewAcceptancePolicyResult(
    bool ShouldIncrementCount,
    Error? Error = null);