namespace Interaction.Application.Models.Results;

public sealed record CommentReportPolicyResult(
    string EvaluatedSeverity,
    int NormalAlertThreshold);