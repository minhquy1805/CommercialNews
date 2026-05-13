using Content.Application.UseCases.ArticleLifecycleEvents.GetArticleLifecycleEvents;
using Content.Application.UseCases.ArticleRevisions.GetArticleRevisionById;
using Content.Application.UseCases.ArticleRevisions.GetArticleRevisions;
using Content.Application.UseCases.ArticleTags.GetArticleTags;
using Content.Application.UseCases.Articles.ArchiveArticle;
using Content.Application.UseCases.Articles.CreateArticle;
using Content.Application.UseCases.Articles.GetArticleById;
using Content.Application.UseCases.Articles.GetArticles;
using Content.Application.UseCases.Articles.PublishArticle;
using Content.Application.UseCases.Articles.SoftDeleteArticle;
using Content.Application.UseCases.Articles.UnpublishArticle;
using Content.Application.UseCases.Articles.UpdateArticle;
using Content.Application.UseCases.Categories.CreateCategory;
using Content.Application.UseCases.Categories.GetCategories;
using Content.Application.UseCases.Categories.GetCategoryById;
using Content.Application.UseCases.Categories.RestoreCategory;
using Content.Application.UseCases.Categories.SoftDeleteCategory;
using Content.Application.UseCases.Categories.UpdateCategory;
using Content.Application.UseCases.Tags.CreateTag;
using Content.Application.UseCases.Tags.GetTagById;
using Content.Application.UseCases.Tags.GetTags;
using Content.Application.UseCases.Tags.RestoreTag;
using Content.Application.UseCases.Tags.SoftDeleteTag;
using Content.Application.UseCases.Tags.UpdateTag;
using Microsoft.Extensions.DependencyInjection;

namespace Content.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Articles
        services.AddScoped<ICreateArticleUseCase, CreateArticleUseCase>();
        services.AddScoped<IUpdateArticleUseCase, UpdateArticleUseCase>();
        services.AddScoped<IGetArticleByIdUseCase, GetArticleByIdUseCase>();
        services.AddScoped<IGetArticlesUseCase, GetArticlesUseCase>();
        services.AddScoped<IPublishArticleUseCase, PublishArticleUseCase>();
        services.AddScoped<IUnpublishArticleUseCase, UnpublishArticleUseCase>();
        services.AddScoped<IArchiveArticleUseCase, ArchiveArticleUseCase>();
        services.AddScoped<ISoftDeleteArticleUseCase, SoftDeleteArticleUseCase>();

        // Article revisions
        services.AddScoped<IGetArticleRevisionsUseCase, GetArticleRevisionsUseCase>();
        services.AddScoped<IGetArticleRevisionByIdUseCase, GetArticleRevisionByIdUseCase>();

        // Article lifecycle events
        services.AddScoped<IGetArticleLifecycleEventsUseCase, GetArticleLifecycleEventsUseCase>();

        // Article tags
        services.AddScoped<IGetArticleTagsUseCase, GetArticleTagsUseCase>();

        // Categories
        services.AddScoped<ICreateCategoryUseCase, CreateCategoryUseCase>();
        services.AddScoped<IUpdateCategoryUseCase, UpdateCategoryUseCase>();
        services.AddScoped<IGetCategoryByIdUseCase, GetCategoryByIdUseCase>();
        services.AddScoped<IGetCategoriesUseCase, GetCategoriesUseCase>();
        services.AddScoped<ISoftDeleteCategoryUseCase, SoftDeleteCategoryUseCase>();
        services.AddScoped<IRestoreCategoryUseCase, RestoreCategoryUseCase>();

        // Tags
        services.AddScoped<ICreateTagUseCase, CreateTagUseCase>();
        services.AddScoped<IUpdateTagUseCase, UpdateTagUseCase>();
        services.AddScoped<IGetTagByIdUseCase, GetTagByIdUseCase>();
        services.AddScoped<IGetTagsUseCase, GetTagsUseCase>();
        services.AddScoped<ISoftDeleteTagUseCase, SoftDeleteTagUseCase>();
        services.AddScoped<IRestoreTagUseCase, RestoreTagUseCase>();

        return services;
    }
}