using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Services;

namespace Interaction.Infrastructure.Services.CommentContent;

public sealed class BlockedTermsCommentContentPolicy
    : ICommentContentPolicy
{
    private static readonly IReadOnlySet<string> BlockedTerms =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "badword"
        };

    public Task<CommentContentPolicyResult> EvaluateAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        foreach (string blockedTerm in BlockedTerms)
        {
            if (content.Contains(
                    blockedTerm,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(
                    CommentContentPolicyResult.Blocked());
            }
        }

        return Task.FromResult(
            CommentContentPolicyResult.Allowed());
    }
}