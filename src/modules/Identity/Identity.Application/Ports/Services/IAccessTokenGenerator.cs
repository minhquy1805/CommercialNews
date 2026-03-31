using Identity.Application.Ports.Services.Models;
using Identity.Domain.Entities;

namespace Identity.Application.Ports.Services
{
    public interface IAccessTokenGenerator
    {
        AccessTokenResult Generate(UserAccount userAccount);
    }
}