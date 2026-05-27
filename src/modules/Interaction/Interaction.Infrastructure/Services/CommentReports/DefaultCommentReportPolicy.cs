using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Services;
using Interaction.Domain.Constants;

namespace Interaction.Infrastructure.Services.CommentReports;

public sealed class DefaultCommentReportPolicy
    : ICommentReportPolicy
{
    private const int DefaultNormalAlertThreshold = 3;

    public Task<CommentReportPolicyResult> EvaluateAsync(
        string reasonCode,
        string? description,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        /*
         * Interaction V1 keeps report evaluation intentionally simple:
         *
         * - Every validated report is treated as Normal severity.
         * - Alert is triggered by the stored procedure once the open case
         *   reaches the configured V1 threshold.
         *
         * ReasonCode validation already belongs to
         * CreateCommentReportValidator before this policy is called.
         *
         * Future versions may map selected reasons to High/Critical
         * without changing CreateCommentReportUseCase.
         */
        _ = description;

        return Task.FromResult(
            new CommentReportPolicyResult(
                EvaluatedSeverity: ReportSeverities.Normal,
                NormalAlertThreshold: DefaultNormalAlertThreshold));
    }
}