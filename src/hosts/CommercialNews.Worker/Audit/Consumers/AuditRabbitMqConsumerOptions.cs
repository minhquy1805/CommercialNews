namespace CommercialNews.Worker.Audit.Consumers;

public sealed class AuditRabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq:AuditConsumer";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "commercialnews";

    public string Password { get; init; } = string.Empty;

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "commercialnews.events";

    public string ExchangeType { get; init; } = "topic";

    public string QueueName { get; init; } = "audit.events";

    public bool DurableQueue { get; init; } = true;

    public bool ExclusiveQueue { get; init; } = false;

    public bool AutoDeleteQueue { get; init; } = false;

    public ushort PrefetchCount { get; init; } = 10;

    public string ConsumerTag { get; init; } = "CommercialNews.Audit.Consumer";

    public string ClientProvidedName { get; init; } = "CommercialNews.Audit.Consumer";

    public bool RequeueOnFailure { get; init; } = true;

    public string[] RoutingKeys { get; init; } =
    [
        "authorization.user_role_assigned",
        "authorization.user_role_revoked",

        "authorization.role_permission_granted",
        "authorization.role_permission_revoked",

        "authorization.role_created",
        "authorization.role_updated",
        "authorization.role_activated",
        "authorization.role_deactivated",

        "authorization.permission_created",
        "authorization.permission_updated",
        "authorization.permission_activated",
        "authorization.permission_deactivated",

        "identity.email_verified",
        "identity.password_changed",

        "identity.user_activated",
        "identity.user_disabled",
        "identity.user_locked",
        "identity.user_unlocked",

        "identity.email_marked_verified",
        "identity.user_sessions_revoked"
    ];
}
