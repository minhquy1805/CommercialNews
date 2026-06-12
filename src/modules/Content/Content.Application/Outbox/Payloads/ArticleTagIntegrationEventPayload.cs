namespace Content.Application.Outbox.Payloads;

public sealed record ArticleTagIntegrationEventPayload(
    long TagId,
    string TagPublicId,
    string Name);
