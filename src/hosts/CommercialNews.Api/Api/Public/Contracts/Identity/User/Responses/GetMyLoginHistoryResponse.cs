namespace CommercialNews.Api.Api.Public.Identity.Contracts.User.Responses
{
    public sealed class GetMyLoginHistoryResponse
    {
        public IReadOnlyList<LoginHistoryItemResponse> Items { get; init; } =
            Array.Empty<LoginHistoryItemResponse>();

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int TotalItems { get; init; }
    }
}
