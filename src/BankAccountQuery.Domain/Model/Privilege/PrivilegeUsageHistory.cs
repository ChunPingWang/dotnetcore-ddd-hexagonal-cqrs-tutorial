using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// PrivilegeUsageHistory — Value Object（優惠使用紀錄查詢結果封裝）。
/// </summary>
public sealed record PrivilegeUsageHistory(
    PrivilegeId PrivilegeId,
    IReadOnlyList<PrivilegeUsageRecord> Records,
    DateRange QueriedRange)
{
    public int Count => Records.Count;
}
