using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence;

public interface ICommentRepository
{
    Task<Comment> InsertVisibleAsync(
        string publicId,
        string articlePublicId,
        long authorUserId,
        string content,
        CancellationToken cancellationToken = default);

    Task<Comment?> GetByPublicIdAsync(
        string commentPublicId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<PublicCommentItemResult>> GetVisibleByArticlePublicIdAsync(
        GetPublicCommentsQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AdminCommentResult>> GetAdminPagedAsync(
        GetAdminCommentsQuery query,
        CancellationToken cancellationToken = default);

    Task<long> GetVisibleCountByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);

    Task<CommentModerationResult> HideAsync(
        string commentPublicId,
        long expectedVersion,
        string historyPublicId,
        long actorUserId,
        string reasonCode,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default);

    Task<CommentModerationResult> RestoreAsync(
        string commentPublicId,
        long expectedVersion,
        string historyPublicId,
        long actorUserId,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default);

    Task<DeleteOwnCommentMutationResult> DeleteOwnAsync(
        string commentPublicId,
        long authorUserId,
        long? expectedVersion,
        string? caseCloseHistoryPublicId,
        string? correlationId,
        CancellationToken cancellationToken = default);
}
