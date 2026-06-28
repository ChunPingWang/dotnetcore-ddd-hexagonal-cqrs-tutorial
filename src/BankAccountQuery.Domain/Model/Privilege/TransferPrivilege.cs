using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// TransferPrivilege — Aggregate Root。
/// 業務規則完全封裝於此。本聚合同時支援兩種持久化：
///   1. 狀態儲存（State-based）：以建構子由目前狀態重建。
///   2. 事件溯源（Event Sourcing）：以 <see cref="Load"/> 重播事件重建，
///      命令方法採「decide（驗證）→ apply（套用事件）」分離。
/// </summary>
public sealed class TransferPrivilege : AggregateRoot
{
    public PrivilegeId PrivilegeId { get; private set; }
    public CustomerId OwnerId { get; private set; }
    public PrivilegeType Type { get; private set; }
    public int TotalQuota { get; private set; }
    public int UsedQuota { get; private set; }
    public DateRange ValidPeriod { get; private set; }

    private readonly List<PrivilegeUsageRecord> _usageRecords;

    public IReadOnlyList<PrivilegeUsageRecord> UsageRecords => _usageRecords.AsReadOnly();

    private TransferPrivilege()
    {
        // EF Core / 事件溯源重播用（狀態由 When(...) 套用）。
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

    // ── 事件溯源：建立（genesis）與重播 ─────────────────────────────────
    /// <summary>核發一筆新優惠（產生創生事件 TransferPrivilegeGrantedEvent）。</summary>
    public static TransferPrivilege Grant(
        PrivilegeId privilegeId,
        CustomerId ownerId,
        PrivilegeType type,
        int totalQuota,
        DateRange validPeriod)
    {
        if (totalQuota < 0)
            throw new ArgumentException("總次數不可為負數", nameof(totalQuota));

        var aggregate = new TransferPrivilege();
        aggregate.RaiseAndApply(new TransferPrivilegeGrantedEvent(
            privilegeId, ownerId, type, totalQuota,
            validPeriod.StartDate, validPeriod.EndDate, DateTime.UtcNow));
        return aggregate;
    }

    /// <summary>以事件序列重播重建聚合（不會再次產生領域事件）。</summary>
    public static TransferPrivilege Load(IEnumerable<IDomainEvent> history)
    {
        var aggregate = new TransferPrivilege();
        foreach (var domainEvent in history)
            aggregate.When(domainEvent);
        return aggregate;
    }

    private void RaiseAndApply(IDomainEvent domainEvent)
    {
        RaiseDomainEvent(domainEvent);
        When(domainEvent);
    }

    /// <summary>套用單一事件以變更狀態（apply）；新事件與重播共用此邏輯。</summary>
    private void When(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case TransferPrivilegeGrantedEvent granted:
                PrivilegeId = granted.PrivilegeId;
                OwnerId = granted.OwnerId;
                Type = granted.Type;
                TotalQuota = granted.TotalQuota;
                UsedQuota = 0;
                ValidPeriod = new DateRange(granted.ValidFrom, granted.ValidTo);
                break;

            case TransferPrivilegeUsedEvent used:
                _usageRecords.Add(new PrivilegeUsageRecord(
                    used.UsageId, used.UsedDate, used.SavedAmount, used.Description));
                UsedQuota += 1;
                break;

            default:
                throw new InvalidOperationException(
                    $"TransferPrivilege 無法套用未知事件：{domainEvent.GetType().Name}");
        }

        Version++;   // 每套用一個事件即推進版本（重播與新事件共用）
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
    // decide：先守護不變量；任一違反則狀態完全不變。
    // apply ：透過 RaiseAndApply 由 When(UsedEvent) 統一變更狀態並記錄事件。
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

        RaiseAndApply(new TransferPrivilegeUsedEvent(
            PrivilegeId, OwnerId, usageId, savedAmount, description,
            GetRemainingQuota() - 1, usedDate, DateTime.UtcNow));

        return _usageRecords[^1];
    }
}
