namespace Authorization.Application.Contracts.Events
{
    public sealed record PermissionCreatedEvent
    {
        public long PermissionId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string NameNormalized { get; init; } = string.Empty;
        public string? Module { get; init; }
        public bool IsSystem { get; init; }
        public bool IsActive { get; init; }
        public long? ActorUserId { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public string? CorrelationId { get; init; }
    }
}