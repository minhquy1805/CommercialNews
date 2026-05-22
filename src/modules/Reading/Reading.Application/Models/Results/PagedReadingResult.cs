namespace Reading.Application.Models.Results;

public sealed class PagedReadingResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalItems { get; init; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalItems / PageSize);
}