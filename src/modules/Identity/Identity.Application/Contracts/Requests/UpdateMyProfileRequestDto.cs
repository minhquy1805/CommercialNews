namespace Identity.Application.Contracts.Requests
{
    public sealed class UpdateMyProfileRequestDto
    {
        public string? FullName { get; init; }
        public string? AvatarUrl { get; init; }
    }
}