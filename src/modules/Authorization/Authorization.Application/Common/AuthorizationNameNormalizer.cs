namespace Authorization.Application.Common;

internal static class AuthorizationNameNormalizer
{
    public static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}