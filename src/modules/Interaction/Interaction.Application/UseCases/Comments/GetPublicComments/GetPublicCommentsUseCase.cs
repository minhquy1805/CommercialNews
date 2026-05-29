using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetPublicComments;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.Comments;

namespace Interaction.Application.UseCases.Comments.GetPublicComments;

public sealed class GetPublicCommentsUseCase : IGetPublicCommentsUseCase
{
    private readonly ICommentRepository _commentRepository;

    public GetPublicCommentsUseCase(
        ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository
            ?? throw new ArgumentNullException(nameof(commentRepository));
    }

    public async Task<Result<PagedQueryResult<GetPublicCommentItemResponseDto>>> ExecuteAsync(
        GetPublicCommentsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = GetPublicCommentsValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<PagedQueryResult<GetPublicCommentItemResponseDto>>.Failure(
                validationError);
        }

        var articlePublicId =
            GetPublicCommentsValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var sortDirection =
            GetPublicCommentsValidator.NormalizeSortDirection(
                request.SortDirection);

        var query = new GetPublicCommentsQuery(
            ArticlePublicId: articlePublicId,
            Page: request.Page,
            PageSize: request.PageSize,
            SortDirection: sortDirection);

        var queryResult =
            await _commentRepository.GetVisibleByArticlePublicIdAsync(
                query,
                cancellationToken);

        var response = new PagedQueryResult<GetPublicCommentItemResponseDto>
        {
            Items = queryResult.Items
                .Select(MapResponseItem)
                .ToArray(),

            Page = queryResult.Page,
            PageSize = queryResult.PageSize,
            TotalItems = queryResult.TotalItems
        };

        return Result<PagedQueryResult<GetPublicCommentItemResponseDto>>.Success(
            response);
    }

    private static GetPublicCommentItemResponseDto MapResponseItem(
        PublicCommentItemResult item)
    {
        return new GetPublicCommentItemResponseDto
        {
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            AuthorUserId = item.AuthorUserId,
            Content = item.Content,
            CreatedAtUtc = item.CreatedAtUtc
        };
    }
}