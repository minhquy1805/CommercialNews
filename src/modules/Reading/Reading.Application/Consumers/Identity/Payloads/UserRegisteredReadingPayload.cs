namespace Reading.Application.Consumers.Identity.Payloads;

public sealed class UserRegisteredReadingPayload
{
    public long UserId { get; init; }

    public string UserPublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public string Status { get; init; } = string.Empty;

    public int Version { get; init; }

    public DateTime RegisteredAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
