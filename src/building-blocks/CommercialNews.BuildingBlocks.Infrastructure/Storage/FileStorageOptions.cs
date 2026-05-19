namespace CommercialNews.BuildingBlocks.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string Provider { get; init; } = "Local";
}