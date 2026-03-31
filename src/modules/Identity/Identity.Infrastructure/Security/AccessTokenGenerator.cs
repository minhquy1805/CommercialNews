using Identity.Application.Ports.Services;
using Identity.Application.Ports.Services.Models;
using Identity.Domain.Entities;

namespace Identity.Infrastructure.Security
{
    public sealed class AccessTokenGenerator : IAccessTokenGenerator
    {
        public AccessTokenResult Generate(UserAccount userAccount)
        {
            return new AccessTokenResult
            {
                AccessToken = $"access-token-for-{userAccount.PublicId}",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            };
        }
    }
}