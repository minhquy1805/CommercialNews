namespace CommercialNews.BuildingBlocks.Infrastructure.Storage.GoogleCloud;

public sealed class GoogleCloudStorageOptions
{
    public const string SectionName = "FileStorage:GoogleCloud";

    public string BucketName { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public string? CredentialsJsonPath { get; init; }

    public bool UsePublicUrl { get; init; } = true;
}