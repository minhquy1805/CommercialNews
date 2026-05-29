namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Requests;

public sealed class GetCommentModerationHistoryRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}