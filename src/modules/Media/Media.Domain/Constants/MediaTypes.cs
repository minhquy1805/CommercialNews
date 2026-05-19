namespace Media.Domain.Constants;

public static class MediaTypes
{
    public const string Image = "Image";
    public const string Video = "Video";
    public const string File = "File";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Image,
        Video,
        File
    };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && All.Contains(value);
    }
}