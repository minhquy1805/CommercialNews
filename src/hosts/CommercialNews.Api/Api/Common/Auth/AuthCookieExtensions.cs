namespace CommercialNews.Api.Api.Common.Auth;

public static class AuthCookieExtensions
{
    public static void SetRefreshTokenCookie(
        this HttpResponse response,
        string refreshToken,
        DateTime expiresAtUtc,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Cookies.Append(
            AuthCookieNames.RefreshToken,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
                Path = "/api/v1/identity",
                IsEssential = true
            });
    }

    public static void ClearRefreshTokenCookie(
        this HttpResponse response,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Cookies.Delete(
            AuthCookieNames.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/api/v1/identity"
            });
    }
}