namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Requests;

public sealed class GetAdminCommentsRequest
{
    public string? Status { get; init; }

    public string? ArticlePublicId { get; init; }

    public long? AuthorUserId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}