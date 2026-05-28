namespace CommercialNews.Api.Api.Public.Contracts.Interaction.CommentReports.Responses;

public sealed class CreateCommentReportResponse
{
    public string CommentReportPublicId { get; init; } = string.Empty;

    public string CommentPublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}