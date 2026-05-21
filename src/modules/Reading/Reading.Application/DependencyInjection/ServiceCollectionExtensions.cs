using Microsoft.Extensions.DependencyInjection;
using Reading.Application.Consumers.Content;
using Reading.Application.UseCases.Articles.GetArticleByPublicId;
using Reading.Application.UseCases.Articles.GetArticleBySlug;
using Reading.Application.UseCases.Articles.GetArticles;
using Reading.Application.UseCases.Articles.GetRelatedArticles;
using Reading.Application.UseCases.Articles.SearchArticles;
using Reading.Application.UseCases.Projections.ApplyContentArticleProjection;
using Reading.Application.UseCases.Projections.MarkArticleProjectionNotPublic;

namespace Reading.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadingApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IGetArticlesUseCase, GetArticlesUseCase>();
        services.AddScoped<IGetArticleByPublicIdUseCase, GetArticleByPublicIdUseCase>();
        services.AddScoped<IGetArticleBySlugUseCase, GetArticleBySlugUseCase>();
        services.AddScoped<IGetRelatedArticlesUseCase, GetRelatedArticlesUseCase>();
        services.AddScoped<ISearchArticlesUseCase, SearchArticlesUseCase>();

        services.AddScoped<IApplyContentArticleProjectionUseCase, ApplyContentArticleProjectionUseCase>();
        services.AddScoped<IMarkArticleProjectionNotPublicUseCase, MarkArticleProjectionNotPublicUseCase>();

        services.AddScoped<IContentReadingEventIngestionService, ContentReadingEventIngestionService>();

        return services;
    }
}