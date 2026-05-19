namespace CommercialNews.BuildingBlocks.Storage.Constants;

public static class FileStorageProviders
{
    public const string Local = "Local";

    public const string GoogleCloud = "GoogleCloud";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Local, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, GoogleCloud, StringComparison.OrdinalIgnoreCase);
    }
}