namespace Interaction.Application.Models.Commands;

public sealed record DismissReportedCommentCaseCommand(
    string CasePublicId,
    long ExpectedCaseVersion,
    string ReasonCode,
    string? Note = null);