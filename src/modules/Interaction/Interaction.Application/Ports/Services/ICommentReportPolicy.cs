using Interaction.Application.Models.Results;

namespace Interaction.Application.Ports.Services;

public interface ICommentReportPolicy
{
    Task<CommentReportPolicyResult> EvaluateAsync(
        string reasonCode,
        string? description,
        CancellationToken cancellationToken = default);
}