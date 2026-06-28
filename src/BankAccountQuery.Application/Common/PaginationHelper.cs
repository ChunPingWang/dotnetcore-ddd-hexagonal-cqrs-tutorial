namespace BankAccountQuery.Application.Common;

/// <summary>
/// 分頁工具（page 為 0-based）。
/// </summary>
public static class PaginationHelper
{
    public static IReadOnlyList<T> Paginate<T>(IReadOnlyList<T> source, int page, int size)
    {
        if (size <= 0) return Array.Empty<T>();
        return source
            .Skip(page * size)
            .Take(size)
            .ToList()
            .AsReadOnly();
    }
}
