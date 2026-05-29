using Interaction.Application.Models.Results;

namespace Interaction.Application.Ports.Persistence;

public interface ICommentReportRepository
{
    Task<CreateCommentReportMutationResult> CreateAsync(
        string reportPublicId,
        string newCasePublicId,
        string commentPublicId,
        long reporterUserId,
        string reasonCode,
        string? description,
        string evaluatedSeverity,
        int normalAlertThreshold,
        string alertMessageIdCandidate,
        CancellationToken cancellationToken = default);
}