namespace Interaction.Application.Models.Results;

public sealed record ProcessPendingViewStatsMaterializationResult(
    int SelectedCount,
    int MaterializedCount,
    int UnchangedCount,
    int FailedCount);