using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetAdminComments;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.Comments;

namespace Interaction.Application.UseCases.Comments.GetAdminComments;

public sealed class GetAdminCommentsUseCase : IGetAdminCommentsUseCase
{
    private readonly ICommentRepository _commentRepository;

    public GetAdminCommentsUseCase(
        ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository
            ?? throw new ArgumentNullException(nameof(commentRepository));
    }

    public async Task<Result<PagedQueryResult<GetAdminCommentItemResponseDto>>> ExecuteAsync(
        GetAdminCommentsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = GetAdminCommentsValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<PagedQueryResult<GetAdminCommentItemResponseDto>>.Failure(
                validationError);
        }

        var query = new GetAdminCommentsQuery(
            Status: GetAdminCommentsValidator.NormalizeStatus(request.Status),
            ArticlePublicId: GetAdminCommentsValidator.NormalizeArticlePublicId(
                request.ArticlePublicId),
            AuthorUserId: request.AuthorUserId,
            Page: request.Page,
            PageSize: request.PageSize);

        var queryResult = await _commentRepository.GetAdminPagedAsync(
            query,
            cancellationToken);

        var response = new PagedQueryResult<GetAdminCommentItemResponseDto>
        {
            Items = queryResult.Items
                .Select(MapResponseItem)
                .ToArray(),

            Page = queryResult.Page,
            PageSize = queryResult.PageSize,
            TotalItems = queryResult.TotalItems
        };

        return Result<PagedQueryResult<GetAdminCommentItemResponseDto>>.Success(
            response);
    }

    private static GetAdminCommentItemResponseDto MapResponseItem(
        AdminCommentResult item)
    {
        return new GetAdminCommentItemResponseDto
        {
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            AuthorUserId = item.AuthorUserId,
            Content = item.Content,
            Status = item.Status,
            ParentCommentPublicId = item.ParentCommentPublicId,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
            DeletedAtUtc = item.DeletedAtUtc,
            Version = item.Version
        };
    }
}