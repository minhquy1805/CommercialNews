using Authorization.Application.Contracts.Ports;

namespace Authorization.Infrastructure.Security
{
    public sealed class AuthorizationOutboxMessageIdGenerator : IOutboxMessageIdGenerator
    {
        public string NewId()
        {
            return Guid.NewGuid()
                .ToString("N")
                .ToUpperInvariant()[..26];
        }
    }
}