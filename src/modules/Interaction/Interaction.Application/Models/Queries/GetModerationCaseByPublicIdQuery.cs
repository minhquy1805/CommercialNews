namespace Interaction.Application.Models.Queries;

public sealed record GetModerationCaseByPublicIdQuery(
    string CasePublicId);