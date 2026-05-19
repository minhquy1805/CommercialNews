using CommercialNews.BuildingBlocks.Infrastructure.Storage.GoogleCloud;
using CommercialNews.BuildingBlocks.Infrastructure.Storage.Local;
using CommercialNews.BuildingBlocks.Storage.Abstractions;
using CommercialNews.BuildingBlocks.Storage.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CommercialNews.BuildingBlocks.Infrastructure.Storage;

public static class FileStorageRegistrationExtensions
{
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));

        services.Configure<LocalFileStorageOptions>(
            configuration.GetSection(LocalFileStorageOptions.SectionName));

        services.Configure<GoogleCloudStorageOptions>(
            configuration.GetSection(GoogleCloudStorageOptions.SectionName));

        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<GoogleCloudFileStorageService>();

        services.AddScoped<IFileStorageService>(serviceProvider =>
        {
            FileStorageOptions options =
                serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

            return options.Provider switch
            {
                FileStorageProviders.Local =>
                    serviceProvider.GetRequiredService<LocalFileStorageService>(),

                FileStorageProviders.GoogleCloud =>
                    serviceProvider.GetRequiredService<GoogleCloudFileStorageService>(),

                _ => throw new InvalidOperationException(
                    $"Unsupported file storage provider '{options.Provider}'.")
            };
        });

        return services;
    }
}