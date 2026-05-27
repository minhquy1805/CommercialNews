using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.GetModerationCases;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.CommentModerationCases;

namespace Interaction.Application.UseCases.CommentModerationCases.GetModerationCases;

public sealed class GetModerationCasesUseCase : IGetModerationCasesUseCase
{
    private readonly ICommentModerationCaseRepository _moderationCaseRepository;

    public GetModerationCasesUseCase(
        ICommentModerationCaseRepository moderationCaseRepository)
    {
        _moderationCaseRepository = moderationCaseRepository
            ?? throw new ArgumentNullException(nameof(moderationCaseRepository));
    }

    public async Task<Result<PagedQueryResult<GetModerationCaseItemResponseDto>>> ExecuteAsync(
        GetModerationCasesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = GetModerationCasesValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<PagedQueryResult<GetModerationCaseItemResponseDto>>.Failure(
                validationError);
        }

        var query = new GetModerationCasesQuery(
            Status: GetModerationCasesValidator.NormalizeStatus(request.Status),
            Priority: GetModerationCasesValidator.NormalizePriority(request.Priority),
            ArticlePublicId: GetModerationCasesValidator.NormalizeArticlePublicId(
                request.ArticlePublicId),
            CommentPublicId: GetModerationCasesValidator.NormalizeCommentPublicId(
                request.CommentPublicId),
            AlertTriggered: request.AlertTriggered,
            Page: request.Page,
            PageSize: request.PageSize);

        var queryResult = await _moderationCaseRepository.GetPagedAsync(
            query,
            cancellationToken);

        var response = new PagedQueryResult<GetModerationCaseItemResponseDto>
        {
            Items = queryResult.Items
                .Select(MapResponseItem)
                .ToArray(),

            Page = queryResult.Page,
            PageSize = queryResult.PageSize,
            TotalItems = queryResult.TotalItems
        };

        return Result<PagedQueryResult<GetModerationCaseItemResponseDto>>.Success(
            response);
    }

    private static GetModerationCaseItemResponseDto MapResponseItem(
        ModerationCaseListItemResult item)
    {
        return new GetModerationCaseItemResponseDto
        {
            CommentModerationCasePublicId = item.CommentModerationCasePublicId,
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            Status = item.Status,
            Priority = item.Priority,
            HighestSeverity = item.HighestSeverity,
            PendingReportCount = item.PendingReportCount,
            DistinctReporterCount = item.DistinctReporterCount,
            AlertTriggered = item.AlertTriggered,
            AlertTriggeredAtUtc = item.AlertTriggeredAtUtc,
            AlertLevel = item.AlertLevel,
            OpenedAtUtc = item.OpenedAtUtc,
            Version = item.Version
        };
    }
}