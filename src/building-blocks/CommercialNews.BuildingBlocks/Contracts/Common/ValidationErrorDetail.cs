namespace CommercialNews.BuildingBlocks.Contracts.Common
{
    public sealed class ValidationErrorDetail
    {
        public string Field { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;
    }
}