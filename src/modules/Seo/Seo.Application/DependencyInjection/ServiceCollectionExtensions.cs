using Microsoft.Extensions.DependencyInjection;
using Seo.Application.UseCases.SeoSettings.CreateSeoMetadata;
using Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataByArticleId;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataById;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataList;
using Seo.Application.UseCases.SeoSettings.UpdateSeoMetadata;
using Seo.Application.UseCases.SeoSettings.UpsertArticleSeoSettings;
using Seo.Application.UseCases.SlugRoutes.ActivateSlugRegistry;
using Seo.Application.UseCases.SlugRoutes.CreateSlugRegistry;
using Seo.Application.UseCases.SlugRoutes.DeactivateSlugRegistry;
using Seo.Application.UseCases.SlugRoutes.GenerateSlug;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByArticleId;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryById;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;
using Seo.Application.UseCases.SlugRoutes.ResolveByScopeAndSlug;
using Seo.Application.UseCases.SlugRoutes.UpdateSlugRegistry;

namespace Seo.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSeoApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IResolveByScopeAndSlugUseCase, ResolveByScopeAndSlugUseCase>();
        services.AddScoped<IGetSlugRegistryByIdUseCase, GetSlugRegistryByIdUseCase>();
        services.AddScoped<IGetSlugRegistryByArticleIdUseCase, GetSlugRegistryByArticleIdUseCase>();
        services.AddScoped<IGetSlugRegistryListUseCase, GetSlugRegistryListUseCase>();
        services.AddScoped<ICreateSlugRegistryUseCase, CreateSlugRegistryUseCase>();
        services.AddScoped<IUpdateSlugRegistryUseCase, UpdateSlugRegistryUseCase>();
        services.AddScoped<IActivateSlugRegistryUseCase, ActivateSlugRegistryUseCase>();
        services.AddScoped<IDeactivateSlugRegistryUseCase, DeactivateSlugRegistryUseCase>();
        services.AddScoped<IGenerateSlugUseCase, GenerateSlugUseCase>();

        services.AddScoped<IGetSeoMetadataByIdUseCase, GetSeoMetadataByIdUseCase>();
        services.AddScoped<IGetSeoMetadataByArticleIdUseCase, GetSeoMetadataByArticleIdUseCase>();
        services.AddScoped<IGetSeoMetadataListUseCase, GetSeoMetadataListUseCase>();
        services.AddScoped<ICreateSeoMetadataUseCase, CreateSeoMetadataUseCase>();
        services.AddScoped<IUpdateSeoMetadataUseCase, UpdateSeoMetadataUseCase>();
        services.AddScoped<IGetArticleSeoSettingsUseCase, GetArticleSeoSettingsUseCase>();
        services.AddScoped<IUpsertArticleSeoSettingsUseCase, UpsertArticleSeoSettingsUseCase>();

        return services;
    }
}