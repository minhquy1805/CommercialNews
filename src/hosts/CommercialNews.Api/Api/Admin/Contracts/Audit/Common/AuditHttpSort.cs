namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

internal sealed record AuditHttpSort(
    string? SortBy,
    string? SortDirection);
