using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;

namespace Identity.Infrastructure.Security
{
    public sealed class AccessTokenGenerator : IAccessTokenGenerator
    {
        public AccessTokenResultDto Generate(UserAccount userAccount)
        {
            return new AccessTokenResultDto
            {
                AccessToken = $"access-token-for-{userAccount.PublicId}",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            };
        }
    }
}
