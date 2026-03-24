namespace Authorization.Application.Contracts.Requests
{
   public sealed class CreateRoleRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsSystem { get; init; }
    } 
}