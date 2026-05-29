using Interaction.Application.Models.Results;

namespace Interaction.Application.Ports.Services;

public interface IArticleViewAcceptancePolicy
{
    Task<ArticleViewAcceptancePolicyResult> EvaluateAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);
}