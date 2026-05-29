using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;

namespace Interaction.Application.Ports.Persistence;

public interface ICommentModerationCaseRepository
{
    Task<PagedQueryResult<ModerationCaseListItemResult>> GetPagedAsync(
        GetModerationCasesQuery query,
        CancellationToken cancellationToken = default);

    Task<ModerationCaseDetailResult?> GetByPublicIdAsync(
        string casePublicId,
        CancellationToken cancellationToken = default);

    Task<DismissReportedCommentCaseResult> DismissAsync(
        string casePublicId,
        long expectedVersion,
        string historyPublicId,
        long actorUserId,
        string reasonCode,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default);

    Task<HideReportedCommentResult> HideCommentAsync(
        string casePublicId,
        long expectedCaseVersion,
        long expectedCommentVersion,
        string historyPublicId,
        long actorUserId,
        string reasonCode,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default);
}