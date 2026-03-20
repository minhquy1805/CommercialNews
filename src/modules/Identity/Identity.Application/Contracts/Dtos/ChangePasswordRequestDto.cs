namespace Identity.Application.Contracts.Dtos
{
    public sealed class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}
