namespace Reading.Application.Models.Commands;

public sealed record ApplyAuthorProfileProjectionCommand(
    long AuthorUserId,
    string AuthorUserPublicId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    long SourceVersion,
    string MessageId,
    DateTime SourceOccurredAtUtc);
