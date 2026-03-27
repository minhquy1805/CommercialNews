namespace Content.Domain.Enums
{
    public static class ArticleStatus
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string Archived = "Archived";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Draft,
            Published,
            Archived
        };

        public static bool IsValid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return All.Contains(value);
        }
    }
}