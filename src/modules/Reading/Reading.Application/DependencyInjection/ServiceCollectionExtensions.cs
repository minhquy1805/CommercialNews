using Microsoft.Extensions.DependencyInjection;
using Reading.Application.UseCases.GetArticleById;
using Reading.Application.UseCases.GetArticleBySlug;
using Reading.Application.UseCases.GetArticles;
using Reading.Application.UseCases.GetRelatedArticles;
using Reading.Application.UseCases.SearchArticles;

namespace Reading.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadingApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IGetArticlesUseCase, GetArticlesUseCase>();
        services.AddScoped<IGetArticleByIdUseCase, GetArticleByIdUseCase>();
        services.AddScoped<IGetArticleBySlugUseCase, GetArticleBySlugUseCase>();
        services.AddScoped<IGetRelatedArticlesUseCase, GetRelatedArticlesUseCase>();
        services.AddScoped<ISearchArticlesUseCase, SearchArticlesUseCase>();

        return services;
    }
}