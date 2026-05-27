namespace Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId;

public sealed class GetModerationCaseByPublicIdRequestDto
{
    public string CasePublicId { get; init; } = string.Empty;
}