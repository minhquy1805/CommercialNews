using Media.Application.UseCases.ArticleMedia.AttachMediaToArticle;
using Media.Application.UseCases.ArticleMedia.DetachMediaFromArticle;
using Media.Application.UseCases.ArticleMedia.GetArticleMediaList;
using Media.Application.UseCases.ArticleMedia.GetArticleMediaSet;
using Media.Application.UseCases.ArticleMedia.GetArticlePrimaryMedia;
using Media.Application.UseCases.ArticleMedia.GetMediaUsage;
using Media.Application.UseCases.ArticleMedia.ReorderArticleMedia;
using Media.Application.UseCases.ArticleMedia.SetPrimaryMedia;
using Media.Application.UseCases.MediaAssets.GetMediaById;
using Media.Application.UseCases.MediaAssets.GetMediaByPublicId;
using Media.Application.UseCases.MediaAssets.GetMediaList;
using Media.Application.UseCases.MediaAssets.RegisterMedia;
using Media.Application.UseCases.MediaAssets.RestoreMedia;
using Media.Application.UseCases.MediaAssets.SoftDeleteMedia;
using Media.Application.UseCases.MediaAssets.UpdateMediaAsset;
using Media.Application.UseCases.MediaAssets.UploadMedia;
using Microsoft.Extensions.DependencyInjection;

namespace Media.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediaApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Media assets
        services.AddScoped<IRegisterMediaUseCase, RegisterMediaUseCase>();
        services.AddScoped<IGetMediaByIdUseCase, GetMediaByIdUseCase>();
        services.AddScoped<IGetMediaByPublicIdUseCase, GetMediaByPublicIdUseCase>();
        services.AddScoped<IGetMediaListUseCase, GetMediaListUseCase>();
        services.AddScoped<IUploadMediaUseCase, UploadMediaUseCase>();
        services.AddScoped<IUpdateMediaAssetUseCase, UpdateMediaAssetUseCase>();
        services.AddScoped<ISoftDeleteMediaUseCase, SoftDeleteMediaUseCase>();
        services.AddScoped<IRestoreMediaUseCase, RestoreMediaUseCase>();

        // Article media reads
        services.AddScoped<IGetArticleMediaSetUseCase, GetArticleMediaSetUseCase>();
        services.AddScoped<IGetArticleMediaListUseCase, GetArticleMediaListUseCase>();
        services.AddScoped<IGetArticlePrimaryMediaUseCase, GetArticlePrimaryMediaUseCase>();
        services.AddScoped<IGetMediaUsageUseCase, GetMediaUsageUseCase>();

        // Article media writes
        services.AddScoped<IAttachMediaToArticleUseCase, AttachMediaToArticleUseCase>();
        services.AddScoped<IDetachMediaFromArticleUseCase, DetachMediaFromArticleUseCase>();
        services.AddScoped<ISetPrimaryMediaUseCase, SetPrimaryMediaUseCase>();
        services.AddScoped<IReorderArticleMediaUseCase, ReorderArticleMediaUseCase>();

        return services;
    }
}