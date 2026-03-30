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
using Content.Application.UseCases.Categories.CreateCategory;
using Content.Application.UseCases.Categories.DeleteCategory;
using Content.Application.UseCases.Categories.GetCategories;
using Content.Application.UseCases.Categories.GetCategoryById;
using Content.Application.UseCases.Categories.RestoreCategory;
using Content.Application.UseCases.Categories.UpdateCategory;
using Content.Application.UseCases.Tags.CreateTag;
using Content.Application.UseCases.Tags.DeleteTag;
using Content.Application.UseCases.Tags.GetTagById;
using Content.Application.UseCases.Tags.GetTags;
using Content.Application.UseCases.Tags.RestoreTag;
using Content.Application.UseCases.Tags.UpdateTag;
using Microsoft.Extensions.DependencyInjection;

namespace Content.Application.DependencyInjection
{
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

            services.AddScoped<ICreateCategoryUseCase, CreateCategoryUseCase>();
            services.AddScoped<IGetCategoryByIdUseCase, GetCategoryByIdUseCase>();
            services.AddScoped<IGetCategoriesUseCase, GetCategoriesUseCase>();
            services.AddScoped<IUpdateCategoryUseCase, UpdateCategoryUseCase>();
            services.AddScoped<IDeleteCategoryUseCase, DeleteCategoryUseCase>();
            services.AddScoped<IRestoreCategoryUseCase, RestoreCategoryUseCase>();

            services.AddScoped<ICreateTagUseCase, CreateTagUseCase>();
            services.AddScoped<IGetTagByIdUseCase, GetTagByIdUseCase>();
            services.AddScoped<IGetTagsUseCase, GetTagsUseCase>();
            services.AddScoped<IUpdateTagUseCase, UpdateTagUseCase>();
            services.AddScoped<IDeleteTagUseCase, DeleteTagUseCase>();
            services.AddScoped<IRestoreTagUseCase, RestoreTagUseCase>();

            return services;
        }
    }
}