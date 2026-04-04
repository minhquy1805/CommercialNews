namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class GetArticlePrimaryMediaResponse
{
    public long ArticleMediaId { get; init; }

    public long ArticleId { get; init; }
    public long MediaId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string StorageProvider { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? StoragePath { get; init; }
    public string? FileName { get; init; }

    public string MediaType { get; init; } = string.Empty;
    public string? MimeType { get; init; }

    public long? FileSizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationSeconds { get; init; }

    public string? DefaultAltText { get; init; }
    public string? AltTextOverride { get; init; }
    public string? Caption { get; init; }

    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public int Version { get; init; }
}