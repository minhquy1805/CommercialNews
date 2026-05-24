namespace CommercialNews.Worker.Reading.Consumers;

public sealed class ReadingRabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq:ReadingConsumer";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "commercialnews";

    public string Password { get; init; } = string.Empty;

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "commercialnews.events";

    public string ExchangeType { get; init; } = "topic";

    public string QueueName { get; init; } = "reading.events";

    public bool DurableQueue { get; init; } = true;

    public bool ExclusiveQueue { get; init; } = false;

    public bool AutoDeleteQueue { get; init; } = false;

    public ushort PrefetchCount { get; init; } = 10;

    public string ConsumerTag { get; init; } = "CommercialNews.Reading.Consumer";

    public string ClientProvidedName { get; init; } = "CommercialNews.Reading.Consumer";

    public bool RequeueOnFailure { get; init; } = true;

    public string[] RoutingKeys { get; init; } =
    [
        "content.article_published",
        "content.article_updated",
        "content.article_unpublished",
        "content.article_archived",
        "content.article_soft_deleted",
        "seo.slug_route_changed",
        "seo.slug_route_deactivated",
        "seo.metadata_updated"
    ];
}
