namespace Interaction.Application.Outbox.Payloads;

public sealed record ArticleLikedPayload(
    string ArticleLikePublicId,
    string ArticlePublicId,
    long UserId,
    DateTime LikedAtUtc);