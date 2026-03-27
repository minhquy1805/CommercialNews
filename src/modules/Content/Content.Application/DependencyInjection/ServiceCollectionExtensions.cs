using Content.Application.UseCases.Articles.ArchiveArticle;
using Content.Application.UseCases.Articles.CreateArticle;
using Content.Application.UseCases.Articles.DeleteArticle;
using Content.Application.UseCases.Articles.GetArticleById;
using Content.Application.UseCases.Articles.GetArticleRevisionById;
using Content.Application.UseCases.Articles.GetArticleRevisions;
using Content.Application.UseCases.Articles.GetArticles;
using Content.Application.UseCases.Articles.PublishArticle;
using Content.Application.UseCases.Articles.RestoreArticle;
using Content.Application.UseCases.Articles.UnpublishArticle;
using Content.Application.UseCases.Articles.UpdateArticle;
using Microsoft.Extensions.DependencyInjection;

namespace Content.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICreateArticleUseCase, CreateArticleUseCase>();
        services.AddScoped<IGetArticleByIdUseCase, GetArticleByIdUseCase>();
        services.AddScoped<IGetArticlesUseCase, GetArticlesUseCase>();
        services.AddScoped<IUpdateArticleUseCase, UpdateArticleUseCase>();
        services.AddScoped<IGetArticleRevisionsUseCase, GetArticleRevisionsUseCase>();
        services.AddScoped<IGetArticleRevisionByIdUseCase, GetArticleRevisionByIdUseCase>();
        services.AddScoped<IPublishArticleUseCase, PublishArticleUseCase>();
        services.AddScoped<IUnpublishArticleUseCase, UnpublishArticleUseCase>();
        services.AddScoped<IArchiveArticleUseCase, ArchiveArticleUseCase>();
        services.AddScoped<IRestoreArticleUseCase, RestoreArticleUseCase>();
        services.AddScoped<IDeleteArticleUseCase, DeleteArticleUseCase>();

        return services;
    }
}