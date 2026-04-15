using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Models.QueryModels;
using Interaction.Application.Ports.Persistence.Read;

namespace Interaction.Application.UseCases.Comments.GetComments;

public sealed class GetCommentsUseCase : IGetCommentsUseCase
{
    private const int MaxPageSize = 100;

    private readonly ICommentQueryRepository _commentQueryRepository;

    public GetCommentsUseCase(ICommentQueryRepository commentQueryRepository)
    {
        _commentQueryRepository = commentQueryRepository;
    }

    public async Task<Result<GetCommentsResponse>> ExecuteAsync(
        GetCommentsRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate the main input first.
        if (request.ArticleId <= 0)
        {
            return Result<GetCommentsResponse>.Failure(
                InteractionErrors.Article.InvalidArticleId);
        }

        if (request.ParentCommentId.HasValue && request.ParentCommentId.Value <= 0)
        {
            return Result<GetCommentsResponse>.Failure(
                InteractionErrors.Comment.InvalidParentCommentId);
        }

        if (request.Page < 1)
        {
            return Result<GetCommentsResponse>.Failure(
                InteractionErrors.Query.InvalidPage);
        }

        if (request.PageSize <= 0)
        {
            return Result<GetCommentsResponse>.Failure(
                InteractionErrors.Query.InvalidPageSize);
        }

        if (request.PageSize > MaxPageSize)
        {
            return Result<GetCommentsResponse>.Failure(
                InteractionErrors.Query.PageSizeTooLarge);
        }

        if (!CommentSortFields.IsValid(request.SortBy))
        {
            return Result<GetCommentsResponse>.Failure(
                InteractionErrors.Query.InvalidSortField);
        }

        string sortDirection = string.IsNullOrWhiteSpace(request.SortDirection)
            ? "DESC"
            : request.SortDirection.Trim().ToUpperInvariant();

        if (sortDirection is not ("ASC" or "DESC"))
        {
            return Result<GetCommentsResponse>.Failure(
                InteractionErrors.Query.InvalidSortField);
        }

        var query = new CommentListQuery
        {
            ArticleId = request.ArticleId,
            ParentCommentId = request.ParentCommentId,
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = request.SortBy.Trim(),
            SortDirection = sortDirection
        };

        PagedQueryResult<CommentListItem> result =
            await _commentQueryRepository.SelectVisibleByArticleIdAsync(
                query,
                cancellationToken);

        var response = new GetCommentsResponse
        {
            Items = result.Items
                .Select(static item => new CommentItemResponse
                {
                    CommentId = item.CommentId,
                    ArticleId = item.ArticleId,
                    UserId = item.UserId,
                    ParentCommentId = item.ParentCommentId,
                    Content = item.Content,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    EditCount = item.EditCount
                })
                .ToArray(),
            PageInfo = new PageInfo
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.PageSize <= 0
                    ? 0
                    : (int)Math.Ceiling((double)result.TotalItems / result.PageSize)
            }
        };

        return Result<GetCommentsResponse>.Success(response);
    }
}