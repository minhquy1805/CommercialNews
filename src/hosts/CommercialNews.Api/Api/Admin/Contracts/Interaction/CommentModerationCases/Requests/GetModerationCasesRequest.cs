namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Requests;

public sealed class GetModerationCasesRequest
{
    public string? Status { get; init; }

    public string? Priority { get; init; }

    public string? ArticlePublicId { get; init; }

    public string? CommentPublicId { get; init; }

    public bool? AlertTriggered { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}