namespace Content.Application.Contracts.Requests
{
    public sealed class DeleteCategoryRequestDto
    {
        public long CategoryId { get; init; }
        public int ExpectedVersion { get; init; }
        public long? ActorUserId { get; init; }
    }
}