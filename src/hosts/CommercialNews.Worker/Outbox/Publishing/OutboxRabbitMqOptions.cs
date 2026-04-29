namespace CommercialNews.Worker.Outbox.Publishing;

public sealed class OutboxRabbitMqOptions
{
    public const string SectionName = "RabbitMq:Outbox";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "commercialnews";

    public string Password { get; init; } = string.Empty;

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "commercialnews.events";

    public string ExchangeType { get; init; } = "topic";

    public bool DurableExchange { get; init; } = true;

    public bool PersistentMessages { get; init; } = true;

    public int PublishTimeoutSeconds { get; init; } = 10;

    public string ClientProvidedName { get; init; } = "CommercialNews.Outbox.Worker";
}