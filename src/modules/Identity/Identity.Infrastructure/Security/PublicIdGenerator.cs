using Identity.Application.Contracts.Ports;
using NUlid;

namespace Identity.Infrastructure.Security
{
    public sealed class PublicIdGenerator : IPublicIdGenerator
    {
        public string NewId()
        {
            return Ulid.NewUlid().ToString();
        }
    }
}
