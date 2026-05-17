namespace CommercialNews.Worker.Seo.Consumers;

public sealed class SeoRabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq:SeoConsumer";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "commercialnews";

    public string Password { get; init; } = string.Empty;

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "commercialnews.events";

    public string ExchangeType { get; init; } = "topic";

    public string QueueName { get; init; } = "seo.events";

    public bool DurableQueue { get; init; } = true;

    public bool ExclusiveQueue { get; init; } = false;

    public bool AutoDeleteQueue { get; init; } = false;

    public ushort PrefetchCount { get; init; } = 10;

    public string ConsumerTag { get; init; } = "CommercialNews.Seo.Consumer";

    public string ClientProvidedName { get; init; } = "CommercialNews.Seo.Consumer";

    public bool RequeueOnFailure { get; init; } = true;

    public string[] RoutingKeys { get; init; } =
    [
        "content.article_created",
        "content.article_updated",
        "content.article_published",
        "content.article_unpublished",
        "content.article_archived",
        "content.article_soft_deleted"
    ];
}