using Reading.Application.Models.Results;

namespace Reading.Application.Ports.Seo;

public interface ISeoRouteResolver
{
    Task<ResolvedSeoRouteResult?> ResolveArticleSlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}