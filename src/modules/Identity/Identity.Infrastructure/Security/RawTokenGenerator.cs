using Identity.Application.Ports.Services;
using System.Security.Cryptography;

namespace Identity.Infrastructure.Security
{
    public sealed class RawTokenGenerator : IRawTokenGenerator
    {
        public string Generate()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
