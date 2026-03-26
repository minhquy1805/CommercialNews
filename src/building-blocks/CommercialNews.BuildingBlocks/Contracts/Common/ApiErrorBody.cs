namespace CommercialNews.BuildingBlocks.Contracts.Common
{
    public sealed class ApiErrorBody
    {
        public string Code { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public IReadOnlyCollection<string> Details { get; init; } = Array.Empty<string>();
    }
}