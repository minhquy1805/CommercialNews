namespace Content.Application.Contracts.Requests
{
    public sealed class GetArticlesRequestDto
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public string? Status { get; init; }
        public long? CategoryId { get; init; }
        public long? TagId { get; init; }

        public string? Sort { get; init; } = "-updatedAt";
    }
}

