using Microsoft.Extensions.DependencyInjection;
using Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataById;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataByResource;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataList;
using Seo.Application.UseCases.SeoSettings.UpsertArticleSeoSettings;
using Seo.Application.UseCases.SlugRoutes.CheckSlugAvailability;
using Seo.Application.UseCases.SlugRoutes.GenerateSlug;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryById;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByResource;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;
using Seo.Application.UseCases.SlugRoutes.ResolveByScopeAndSlug;

namespace Seo.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSeoApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IResolveByScopeAndSlugUseCase, ResolveByScopeAndSlugUseCase>();
        services.AddScoped<IGetSlugRegistryByIdUseCase, GetSlugRegistryByIdUseCase>();
        services.AddScoped<IGetSlugRegistryByResourceUseCase, GetSlugRegistryByResourceUseCase>();
        services.AddScoped<IGetSlugRegistryListUseCase, GetSlugRegistryListUseCase>();
        services.AddScoped<ICheckSlugAvailabilityUseCase, CheckSlugAvailabilityUseCase>();
        services.AddScoped<IGenerateSlugUseCase, GenerateSlugUseCase>();

        services.AddScoped<IGetSeoMetadataByIdUseCase, GetSeoMetadataByIdUseCase>();
        services.AddScoped<IGetSeoMetadataByResourceUseCase, GetSeoMetadataByResourceUseCase>();
        services.AddScoped<IGetSeoMetadataListUseCase, GetSeoMetadataListUseCase>();
        services.AddScoped<IGetArticleSeoSettingsUseCase, GetArticleSeoSettingsUseCase>();
        services.AddScoped<IUpsertArticleSeoSettingsUseCase, UpsertArticleSeoSettingsUseCase>();

        return services;
    }
}
