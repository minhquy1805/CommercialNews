using Authorization.Application.Contracts.Ports;

namespace Authorization.Infrastructure.Security
{
    public sealed class AuthorizationPublicIdGenerator : IPublicIdGenerator
    {
        public string NewId()
        {
            return Guid.NewGuid()
                .ToString("N")
                .ToUpperInvariant()[..26];
        }
    }
}