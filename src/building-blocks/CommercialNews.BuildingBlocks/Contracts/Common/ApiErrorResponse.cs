namespace CommercialNews.BuildingBlocks.Contracts.Common
{
    public sealed class ApiErrorResponse
    {
        public string TraceId { get; init; } = string.Empty;

        public ApiErrorBody Error { get; init; } = new();
    }
}

