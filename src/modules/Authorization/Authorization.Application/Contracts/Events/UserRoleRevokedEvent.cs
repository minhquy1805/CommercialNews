namespace Authorization.Application.Contracts.Events
{
    public sealed record UserRoleRevokedEvent
    {
        public long UserRoleId { get; init; }
        public long TargetUserId { get; init; }
        public long RoleId { get; init; }
        public long? ActorUserId { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public string? CorrelationId { get; init; }
    }
}