using Interaction.Application.Models.Results;

namespace Interaction.Application.Ports.Services;

public interface ICommentContentPolicy
{
    Task<CommentContentPolicyResult> EvaluateAsync(
        string content,
        CancellationToken cancellationToken = default);
}