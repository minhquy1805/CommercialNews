using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Responses;

public sealed class GetCommentsHttpResponse
{
    public IReadOnlyList<CommentItemHttpResponse> Items { get; init; } = Array.Empty<CommentItemHttpResponse>();

    public PageInfo PageInfo { get; init; } = new();
}