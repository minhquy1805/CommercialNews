namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class GetMediaUsageItemResponse
{
    public long ArticleMediaId { get; init; }

    public long ArticleId { get; init; }
    public int AttachmentSetVersion { get; init; }

    public long MediaId { get; init; }

    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }

    public string? AltTextOverride { get; init; }
    public string? Caption { get; init; }

    public DateTime CreatedAt { get; init; }
    public long? CreatedBy { get; init; }

    public DateTime UpdatedAt { get; init; }
    public long? UpdatedBy { get; init; }

    public int Version { get; init; }

    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }
    public long? DeletedBy { get; init; }
}