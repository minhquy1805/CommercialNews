namespace CommercialNews.Worker.Interaction.Consumers;

public sealed class InteractionRabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq:InteractionConsumer";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "commercialnews";

    public string Password { get; init; } = string.Empty;

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "commercialnews.events";

    public string ExchangeType { get; init; } = "topic";

    public string QueueName { get; init; } = "interaction.events";

    public bool DurableQueue { get; init; } = true;

    public bool ExclusiveQueue { get; init; } = false;

    public bool AutoDeleteQueue { get; init; } = false;

    public ushort PrefetchCount { get; init; } = 10;

    public string ConsumerTag { get; init; } = "CommercialNews.Interaction.Consumer";

    public string ClientProvidedName { get; init; } = "CommercialNews.Interaction.Consumer";

    public bool RequeueOnFailure { get; init; } = true;

    public string[] RoutingKeys { get; init; } = [];
}
