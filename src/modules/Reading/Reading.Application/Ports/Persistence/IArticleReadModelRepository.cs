using Reading.Application.Models.Commands;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;

namespace Reading.Application.Ports.Persistence;

public interface IArticleReadModelRepository
{
    Task<ArticleDetailResult?> SelectByPublicIdAsync(
        GetArticleByPublicIdQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedReadingResult<ArticleListItemResult>> SelectSkipAndTakeAsync(
        GetArticlesQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedReadingResult<ArticleListItemResult>> SearchAsync(
        SearchArticlesQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticleListItemResult>> SelectRelatedAsync(
        GetRelatedArticlesQuery query,
        CancellationToken cancellationToken = default);

    Task<ArticleProjectionApplyResult> UpsertFromContentAsync(
        ApplyContentArticleProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleProjectionApplyResult> MarkNotPublicAsync(
        MarkArticleProjectionNotPublicCommand command,
        CancellationToken cancellationToken = default);
}