namespace Authorization.Application.Contracts.Events
{
    public sealed record RoleDeactivatedEvent
    {
        public long RoleId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public long? ActorUserId { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public string? CorrelationId { get; init; }
    }
}