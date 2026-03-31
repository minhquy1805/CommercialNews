using System.Security.Cryptography;
using System.Text;
using Identity.Application.Ports.Services;

namespace Identity.Infrastructure.Security
{
    public sealed class TokenHashProvider : ITokenHashProvider
    {
        public byte[] Hash(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentException("Raw token is required.", nameof(rawToken));

            return SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        }
    }
}
