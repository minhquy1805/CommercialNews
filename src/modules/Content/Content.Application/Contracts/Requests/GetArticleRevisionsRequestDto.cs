namespace Content.Application.Contracts.Requests
{
   public sealed class GetArticleRevisionsRequestDto
    {
        public long ArticleId { get; init; }

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 20;
    } 
}

