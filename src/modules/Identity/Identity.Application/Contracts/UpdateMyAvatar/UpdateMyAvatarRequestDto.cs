namespace Identity.Application.Contracts.UpdateMyAvatar;

public sealed class UpdateMyAvatarRequestDto
{
    public Stream Content { get; init; } = Stream.Null;

    public string OriginalFileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public long Length { get; init; }
}
