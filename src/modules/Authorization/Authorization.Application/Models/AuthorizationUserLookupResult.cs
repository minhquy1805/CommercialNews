namespace Authorization.Application.Models;

public sealed record AuthorizationUserLookupResult(
    long UserId,
    string PublicId,
    string Email);