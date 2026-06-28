using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Application.Queries.Privilege.Results;

/// <summary>
/// 優惠使用紀錄 Read Model。
/// </summary>
public sealed record PrivilegeUsageHistoryResult(
    string PrivilegeId,
    IReadOnlyList<PrivilegeUsageDto> Records,
    PageInfo PageInfo)
{
    public static PrivilegeUsageHistoryResult From(
        PrivilegeUsageHistory history, int page, int size)
    {
        var dtos = history.Records
            .Select(PrivilegeUsageDto.From)
            .ToList();

        return new PrivilegeUsageHistoryResult(
            history.PrivilegeId.Value,
            PaginationHelper.Paginate(dtos, page, size),
            PageInfo.Of(page, size, dtos.Count));
    }
}
