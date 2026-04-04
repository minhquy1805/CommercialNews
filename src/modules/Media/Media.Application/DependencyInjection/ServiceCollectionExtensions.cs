using Media.Application.UseCases.ArticleMedia.AttachMediaToArticle;
using Media.Application.UseCases.ArticleMedia.DetachMediaFromArticle;
using Media.Application.UseCases.ArticleMedia.GetArticleMediaList;
using Media.Application.UseCases.ArticleMedia.GetArticlePrimaryMedia;
using Media.Application.UseCases.ArticleMedia.ReorderArticleMedia;
using Media.Application.UseCases.ArticleMedia.RestoreArticleMedia;
using Media.Application.UseCases.ArticleMedia.SetPrimaryMedia;
using Media.Application.UseCases.MediaAssets.GetMediaById;
using Media.Application.UseCases.MediaAssets.GetMediaByPublicId;
using Media.Application.UseCases.MediaAssets.GetMediaList;
using Media.Application.UseCases.MediaAssets.RegisterMedia;
using Media.Application.UseCases.MediaAssets.RestoreMedia;
using Media.Application.UseCases.MediaAssets.SoftDeleteMedia;
using Microsoft.Extensions.DependencyInjection;

namespace Media.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediaApplication(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<IRegisterMediaUseCase, RegisterMediaUseCase>();
            services.AddScoped<IGetMediaByIdUseCase, GetMediaByIdUseCase>();
            services.AddScoped<IGetMediaByPublicIdUseCase, GetMediaByPublicIdUseCase>();
            services.AddScoped<IGetMediaListUseCase, GetMediaListUseCase>();
            services.AddScoped<ISoftDeleteMediaUseCase, SoftDeleteMediaUseCase>();
            services.AddScoped<IRestoreMediaUseCase, RestoreMediaUseCase>();

            services.AddScoped<IAttachMediaToArticleUseCase, AttachMediaToArticleUseCase>();
            services.AddScoped<IDetachMediaFromArticleUseCase, DetachMediaFromArticleUseCase>();
            services.AddScoped<IRestoreArticleMediaUseCase, RestoreArticleMediaUseCase>();
            services.AddScoped<ISetPrimaryMediaUseCase, SetPrimaryMediaUseCase>();
            services.AddScoped<IReorderArticleMediaUseCase, ReorderArticleMediaUseCase>();
            services.AddScoped<IGetArticleMediaListUseCase, GetArticleMediaListUseCase>();
            services.AddScoped<IGetArticlePrimaryMediaUseCase, GetArticlePrimaryMediaUseCase>();

            return services;
        }
    }
}