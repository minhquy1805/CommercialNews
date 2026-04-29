namespace CommercialNews.Worker.Notifications.Consumers;

public sealed class NotificationsRabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq:NotificationsConsumer";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "commercialnews";

    public string Password { get; init; } = string.Empty;

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "commercialnews.events";

    public string ExchangeType { get; init; } = "topic";

    public string QueueName { get; init; } = "notifications.identity.email";

    public bool DurableQueue { get; init; } = true;

    public bool ExclusiveQueue { get; init; } = false;

    public bool AutoDeleteQueue { get; init; } = false;

    public ushort PrefetchCount { get; init; } = 10;

    public string ConsumerTag { get; init; } = "CommercialNews.Notifications.Consumer";

    public string ClientProvidedName { get; init; } = "CommercialNews.Notifications.Consumer";

    public bool RequeueOnFailure { get; init; } = true;

    public string[] RoutingKeys { get; init; } =
    [
        "identity.verification_email_requested",
        "identity.password_reset_requested"
    ];
}