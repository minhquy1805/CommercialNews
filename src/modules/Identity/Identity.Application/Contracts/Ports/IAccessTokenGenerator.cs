using Identity.Application.Contracts.Responses;
using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Ports
{
    public interface IAccessTokenGenerator
    {
        AccessTokenResultDto Generate(UserAccount userAccount);
    }
}
