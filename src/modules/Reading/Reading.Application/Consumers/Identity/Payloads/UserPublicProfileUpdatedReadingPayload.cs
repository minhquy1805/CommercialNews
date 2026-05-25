namespace Reading.Application.Consumers.Identity.Payloads;

public sealed class UserPublicProfileUpdatedReadingPayload
{
    public long UserId { get; init; }

    public string UserPublicId { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public string? AvatarUrl { get; init; }

    public int Version { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
