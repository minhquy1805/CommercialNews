namespace Authorization.Application.Contracts.Events
{
    public sealed record RolePermissionGrantedEvent
    {
        public long RolePermissionId { get; init; }
        public long RoleId { get; init; }
        public long PermissionId { get; init; }
        public long? ActorUserId { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public string? CorrelationId { get; init; }
    }
}