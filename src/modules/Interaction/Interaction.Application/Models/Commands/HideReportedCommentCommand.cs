namespace Interaction.Application.Models.Commands;

public sealed record HideReportedCommentCommand(
    string CasePublicId,
    long ExpectedCaseVersion,
    long ExpectedCommentVersion,
    string ReasonCode,
    string? Note = null);