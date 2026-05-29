namespace Interaction.Application.Outbox.Payloads;

public sealed record ArticleUnlikedPayload(
    string ArticleLikePublicId,
    string ArticlePublicId,
    long UserId,
    DateTime UnlikedAtUtc);