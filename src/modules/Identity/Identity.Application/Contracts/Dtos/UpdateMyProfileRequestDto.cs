namespace Identity.Application.Contracts.Dtos
{
    public sealed class UpdateMyProfileRequestDto
    {
        public string? FullName { get; init; }
        public string? AvatarUrl { get; init; }
    }
}