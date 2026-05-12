namespace Content.Domain.Common;

public static class ContentText
{
    public static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
