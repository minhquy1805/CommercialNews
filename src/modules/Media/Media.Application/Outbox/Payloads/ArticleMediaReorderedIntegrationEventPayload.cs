namespace Media.Application.Outbox.Payloads;

public sealed record ArticleMediaReorderedIntegrationEventPayload(
    long ArticleId,
    IReadOnlyCollection<ArticleMediaReorderedItem> Items,
    long ActorUserId,
    long AttachmentSetVersion,
    DateTime ReorderedAtUtc,
    string BusinessDedupeKey);

public sealed record ArticleMediaReorderedItem(
    long MediaId,
    int SortOrder);