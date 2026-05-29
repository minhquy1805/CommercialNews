using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Services;

namespace Interaction.Infrastructure.Services.ArticleViews;

public sealed class AllowAllArticleViewAcceptancePolicy
    : IArticleViewAcceptancePolicy
{
    public Task<ArticleViewAcceptancePolicyResult> EvaluateAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        /*
         * Initial Interaction V1 implementation.
         *
         * Every structurally valid view request is accepted for counting.
         * Duplicate-view suppression and abuse prevention are intentionally
         * deferred until a cache-backed policy is introduced.
         *
         * This class exists so TrackArticleViewUseCase depends on a stable
         * policy abstraction rather than hardcoding the counting decision.
         */
        return Task.FromResult(
            new ArticleViewAcceptancePolicyResult(
                ShouldIncrementCount: true,
                Error: null));
    }
}