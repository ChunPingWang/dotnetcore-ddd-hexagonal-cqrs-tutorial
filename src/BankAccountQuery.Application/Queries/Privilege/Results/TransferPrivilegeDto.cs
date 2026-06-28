using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Application.Queries.Privilege.Results;

/// <summary>
/// 優惠方案 Read Model（從 Domain Aggregate 讀取計算後的業務狀態）。
/// </summary>
public sealed record TransferPrivilegeDto(
    string PrivilegeId,
    string PrivilegeType,
    int TotalQuota,
    int UsedQuota,
    int RemainingQuota,     // Domain Method: GetRemainingQuota()
    string ValidFrom,
    string ValidTo,
    bool IsValid)           // Domain Method: IsValid()
{
    public static TransferPrivilegeDto From(TransferPrivilege p) => new(
        p.PrivilegeId.Value,
        p.Type.ToString(),
        p.TotalQuota,
        p.UsedQuota,
        p.GetRemainingQuota(),
        p.ValidPeriod.StartDate.ToString("yyyy-MM-dd"),
        p.ValidPeriod.EndDate.ToString("yyyy-MM-dd"),
        p.IsValid());
}
