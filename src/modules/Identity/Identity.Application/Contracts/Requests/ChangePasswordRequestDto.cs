namespace Identity.Application.Contracts.Requests
{
    public sealed class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}
