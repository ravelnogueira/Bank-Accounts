namespace Bank.Accounts.Application.Accounts.DTOs;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static PagedResponse<T> Create(IReadOnlyCollection<T> items, int page, int pageSize, int totalItems) =>
        new(items, page, pageSize, totalItems, totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize));
}