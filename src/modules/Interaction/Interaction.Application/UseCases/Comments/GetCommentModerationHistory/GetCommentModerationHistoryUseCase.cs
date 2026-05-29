using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetCommentModerationHistory;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.Comments;

namespace Interaction.Application.UseCases.Comments.GetCommentModerationHistory;

public sealed class GetCommentModerationHistoryUseCase
    : IGetCommentModerationHistoryUseCase
{
    private readonly ICommentModerationActionHistoryRepository _historyRepository;

    public GetCommentModerationHistoryUseCase(
        ICommentModerationActionHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository
            ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    public async Task<Result<PagedQueryResult<GetCommentModerationHistoryItemResponseDto>>> ExecuteAsync(
        GetCommentModerationHistoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            GetCommentModerationHistoryValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<PagedQueryResult<GetCommentModerationHistoryItemResponseDto>>.Failure(
                validationError);
        }

        var commentPublicId =
            GetCommentModerationHistoryValidator.NormalizeCommentPublicId(
                request.CommentPublicId);

        var query = new GetCommentModerationHistoryQuery(
            CommentPublicId: commentPublicId,
            Page: request.Page,
            PageSize: request.PageSize);

        var queryResult = await _historyRepository.GetByCommentPublicIdAsync(
            query,
            cancellationToken);

        var response = new PagedQueryResult<GetCommentModerationHistoryItemResponseDto>
        {
            Items = queryResult.Items
                .Select(MapResponseItem)
                .ToArray(),

            Page = queryResult.Page,
            PageSize = queryResult.PageSize,
            TotalItems = queryResult.TotalItems
        };

        return Result<PagedQueryResult<GetCommentModerationHistoryItemResponseDto>>.Success(
            response);
    }

    private static GetCommentModerationHistoryItemResponseDto MapResponseItem(
        CommentModerationHistoryItemResult item)
    {
        return new GetCommentModerationHistoryItemResponseDto
        {
            HistoryPublicId = item.HistoryPublicId,
            CommentPublicId = item.CommentPublicId,
            CommentModerationCasePublicId = item.CommentModerationCasePublicId,
            ActionType = item.ActionType,
            FromStatus = item.FromStatus,
            ToStatus = item.ToStatus,
            ActorUserId = item.ActorUserId,
            ActorType = item.ActorType,
            ReasonCode = item.ReasonCode,
            Note = item.Note,
            OccurredAtUtc = item.OccurredAtUtc,
            CorrelationId = item.CorrelationId
        };
    }
}