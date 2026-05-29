namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Requests;

public sealed class GetPublicCommentsRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string SortDirection { get; init; } = "DESC";
}