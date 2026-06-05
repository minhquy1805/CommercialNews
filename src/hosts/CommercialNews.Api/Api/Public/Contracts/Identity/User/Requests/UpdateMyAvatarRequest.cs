using Microsoft.AspNetCore.Http;

namespace CommercialNews.Api.Api.Public.Identity.Contracts.User.Requests
{
    public sealed class UpdateMyAvatarRequest
    {
        public IFormFile File { get; init; } = default!;
    }
}
