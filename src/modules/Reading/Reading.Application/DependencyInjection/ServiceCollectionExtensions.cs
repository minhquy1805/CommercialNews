using Microsoft.Extensions.DependencyInjection;
using Reading.Application.Consumers.Content;
using Reading.Application.Consumers.Media;
using Reading.Application.Consumers.Seo;
using Reading.Application.UseCases.Articles.GetArticleByPublicId;
using Reading.Application.UseCases.Articles.GetArticleBySlug;
using Reading.Application.UseCases.Articles.GetArticles;
using Reading.Application.UseCases.Articles.GetRelatedArticles;
using Reading.Application.UseCases.Articles.SearchArticles;
using Reading.Application.UseCases.Projections.ApplyContentArticleProjection;
using Reading.Application.UseCases.Projections.ApplyArticleMediaProjection;
using Reading.Application.UseCases.Projections.ApplyArticleSeoMetadataProjection;
using Reading.Application.UseCases.Projections.ApplyArticleSeoRouteProjection;
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
        services.AddScoped<IApplyArticleMediaProjectionUseCase, ApplyArticleMediaProjectionUseCase>();
        services.AddScoped<IApplyArticleSeoRouteProjectionUseCase, ApplyArticleSeoRouteProjectionUseCase>();
        services.AddScoped<IApplyArticleSeoMetadataProjectionUseCase, ApplyArticleSeoMetadataProjectionUseCase>();
        services.AddScoped<IMarkArticleProjectionNotPublicUseCase, MarkArticleProjectionNotPublicUseCase>();

        services.AddScoped<IContentReadingEventIngestionService, ContentReadingEventIngestionService>();
        services.AddScoped<IMediaReadingEventIngestionService, MediaReadingEventIngestionService>();
        services.AddScoped<ISeoReadingEventIngestionService, SeoReadingEventIngestionService>();

        return services;
    }
}
