namespace PayderPay.Application.Common.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public static PagedResult<T> Empty(PageRequest page) =>
        new(Array.Empty<T>(), page.NormalizedPage, page.NormalizedPageSize, 0, 0);

    public static PagedResult<T> From(IReadOnlyList<T> items, int totalCount, PageRequest page)
    {
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)page.NormalizedPageSize);

        return new PagedResult<T>(items, page.NormalizedPage, page.NormalizedPageSize, totalCount, totalPages);
    }

    public PagedResult<TOut> Map<TOut>(IReadOnlyList<TOut> mappedItems) =>
        new(mappedItems, Page, PageSize, TotalCount, TotalPages);
}
