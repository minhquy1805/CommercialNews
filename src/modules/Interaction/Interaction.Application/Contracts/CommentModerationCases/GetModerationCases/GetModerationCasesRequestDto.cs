namespace Interaction.Application.Contracts.CommentModerationCases.GetModerationCases;

public sealed class GetModerationCasesRequestDto
{
    public string? Status { get; init; }

    public string? Priority { get; init; }

    public string? ArticlePublicId { get; init; }

    public string? CommentPublicId { get; init; }

    public bool? AlertTriggered { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}