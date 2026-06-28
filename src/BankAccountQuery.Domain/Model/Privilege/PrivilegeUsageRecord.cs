using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// PrivilegeUsageRecord — Entity（屬於 TransferPrivilege Aggregate 邊界內）。
/// </summary>
public sealed class PrivilegeUsageRecord
{
    public string UsageId { get; }
    public DateOnly UsedDate { get; }
    public Money SavedAmount { get; }     // 本次優惠節省金額
    public string Description { get; }

    public PrivilegeUsageRecord(
        string usageId,
        DateOnly usedDate,
        Money savedAmount,
        string description)
    {
        if (string.IsNullOrWhiteSpace(usageId))
            throw new ArgumentException("UsageId 不可為空", nameof(usageId));
        UsageId = usageId;
        UsedDate = usedDate;
        SavedAmount = savedAmount;
        Description = description ?? string.Empty;
    }
}
