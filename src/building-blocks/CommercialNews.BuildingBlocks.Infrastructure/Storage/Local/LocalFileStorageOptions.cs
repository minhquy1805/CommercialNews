namespace CommercialNews.BuildingBlocks.Infrastructure.Storage.Local;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "FileStorage:Local";

    public string RootPath { get; init; } = "wwwroot/uploads";

    public string PublicBaseUrl { get; init; } = "http://localhost:5226/uploads";
}