using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// TransferPrivilege — Aggregate Root。
/// 優惠有效性、剩餘次數、使用紀錄過濾等業務規則完全封裝於此。
/// </summary>
public sealed class TransferPrivilege : AggregateRoot
{
    public PrivilegeId PrivilegeId { get; }
    public CustomerId OwnerId { get; }
    public PrivilegeType Type { get; }
    public int TotalQuota { get; }
    public int UsedQuota { get; private set; }
    public DateRange ValidPeriod { get; }

    private readonly List<PrivilegeUsageRecord> _usageRecords;

    public IReadOnlyList<PrivilegeUsageRecord> UsageRecords => _usageRecords.AsReadOnly();

    private TransferPrivilege()
    {
        // EF Core 用。
        PrivilegeId = null!;
        OwnerId = null!;
        ValidPeriod = null!;
        _usageRecords = new List<PrivilegeUsageRecord>();
    }

    public TransferPrivilege(
        PrivilegeId privilegeId,
        CustomerId ownerId,
        PrivilegeType type,
        int totalQuota,
        int usedQuota,
        DateRange validPeriod,
        IEnumerable<PrivilegeUsageRecord>? usageRecords = null)
    {
        if (totalQuota < 0)
            throw new ArgumentException("總次數不可為負數", nameof(totalQuota));
        if (usedQuota < 0 || usedQuota > totalQuota)
            throw new ArgumentException("已用次數需介於 0 與總次數之間", nameof(usedQuota));

        PrivilegeId = privilegeId;
        OwnerId = ownerId;
        Type = type;
        TotalQuota = totalQuota;
        UsedQuota = usedQuota;
        ValidPeriod = validPeriod;
        _usageRecords = usageRecords?.ToList() ?? new List<PrivilegeUsageRecord>();
    }

    // ── 業務規則 1：優惠是否有效 ────────────────────────────────────────
    public bool IsValid() => IsWithinValidPeriod() && HasRemainingQuota();

    private bool IsWithinValidPeriod() =>
        ValidPeriod.Contains(DateOnly.FromDateTime(DateTime.Today));

    private bool HasRemainingQuota() => GetRemainingQuota() > 0;

    // ── 業務規則 2：剩餘次數 ────────────────────────────────────────────
    public int GetRemainingQuota() => TotalQuota - UsedQuota;

    // ── 業務規則 3：所有權驗證 ──────────────────────────────────────────
    public void VerifyOwnership(CustomerId requesterId)
    {
        if (OwnerId != requesterId)
            throw new PrivilegeNotOwnedByCustomerException(PrivilegeId, requesterId);
    }

    // ── 業務規則 4：使用紀錄過濾 ────────────────────────────────────────
    public PrivilegeUsageHistory FilterUsageHistory(DateRange dateRange)
    {
        var filtered = _usageRecords
            .Where(r => dateRange.Contains(r.UsedDate))
            .OrderByDescending(r => r.UsedDate)
            .ToList()
            .AsReadOnly();

        return new PrivilegeUsageHistory(PrivilegeId, filtered, dateRange);
    }

    // ── 業務規則 5（寫入側）：使用一次優惠 ──────────────────────────────
    // 這是 DDD 寫入側的核心：聚合在「改變狀態」時強制守護不變量。
    // 任一不變量違反，狀態完全不變（不會新增紀錄、不會加計次數）。
    public PrivilegeUsageRecord Use(
        string usageId,
        Money savedAmount,
        string description,
        DateOnly usedDate)
    {
        // 不變量 1：必須在有效期間內
        if (!ValidPeriod.Contains(usedDate))
            throw new PrivilegeExpiredException(PrivilegeId);

        // 不變量 2：必須仍有剩餘次數
        if (GetRemainingQuota() <= 0)
            throw new PrivilegeQuotaExhaustedException(PrivilegeId);

        var record = new PrivilegeUsageRecord(usageId, usedDate, savedAmount, description);
        _usageRecords.Add(record);
        UsedQuota += 1;

        // 發布領域事件（由應用層在持久化成功後派發）
        RaiseDomainEvent(new TransferPrivilegeUsedEvent(
            PrivilegeId, OwnerId, usageId, savedAmount,
            GetRemainingQuota(), usedDate, DateTime.UtcNow));

        return record;
    }
}
