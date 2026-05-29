using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.ArticleInteractionStats;

public static class ProcessPendingViewStatsMaterializationValidator
{
    public const int MaximumBatchSize = 500;

    public static Error? Validate(int batchSize)
    {
        if (batchSize < 1 || batchSize > MaximumBatchSize)
        {
            return InteractionErrors.Counter.InvalidViewStatsMaterializationBatchSize;
        }

        return null;
    }
}