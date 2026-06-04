namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

internal static class AuditHttpSortParser
{
    public static AuditHttpSort Parse(
        string? sort,
        IReadOnlyDictionary<string, string> allowedFields)
    {
        var normalizedSort = sort?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSort))
        {
            return new AuditHttpSort(null, null);
        }

        var direction = "asc";
        if (normalizedSort[0] == '-')
        {
            direction = "desc";
            normalizedSort = normalizedSort[1..].Trim();
        }
        else if (normalizedSort[0] == '+')
        {
            normalizedSort = normalizedSort[1..].Trim();
        }

        if (string.IsNullOrWhiteSpace(normalizedSort))
        {
            return new AuditHttpSort(null, null);
        }

        return new AuditHttpSort(
            allowedFields.TryGetValue(normalizedSort, out var sortBy)
                ? sortBy
                : normalizedSort,
            direction);
    }
}
