namespace BankAccountQuery.Application.Queries.Account.Results;

/// <summary>
/// 分頁資訊 Read Model。
/// </summary>
public sealed record PageInfo(int Page, int Size, int TotalCount, int TotalPages)
{
    public static PageInfo Of(int page, int size, int totalCount)
    {
        var totalPages = size <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)size);
        return new PageInfo(page, size, totalCount, totalPages);
    }

    public static PageInfo Empty => new(0, 0, 0, 0);
}
