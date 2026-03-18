namespace Identity.Application.Contracts.Dtos
{
    public sealed class RegisterUserRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? FullName { get; init; }
    }
}
