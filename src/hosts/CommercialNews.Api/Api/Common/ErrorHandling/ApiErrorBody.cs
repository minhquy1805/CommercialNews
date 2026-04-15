namespace CommercialNews.Api.Api.Common.ErrorHandling;

public sealed class ApiErrorBody
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();
}