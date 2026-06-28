using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Account;

/// <summary>
/// Account — Aggregate Root。
/// 業務規則完全封裝於此，Application Layer 只協調，不判斷。
/// </summary>
public sealed class Account
{
    public AccountId AccountId { get; }
    public CustomerId OwnerId { get; }
    public AccountType AccountType { get; }       // Twd / Fx
    public Currency Currency { get; }
    public AccountStatus Status { get; private set; }

    private Account()
    {
        // EF Core 用；以 null! 避免 nullable 警告，實際由 EF 填值。
        AccountId = null!;
        OwnerId = null!;
    }

    public Account(
        AccountId accountId,
        CustomerId ownerId,
        AccountType accountType,
        Currency currency,
        AccountStatus status)
    {
        AccountId = accountId;
        OwnerId = ownerId;
        AccountType = accountType;
        Currency = currency;
        Status = status;
    }

    // ── 業務規則 1：所有權驗證 ──────────────────────────────────────────
    public void VerifyOwnership(CustomerId requesterId)
    {
        if (OwnerId != requesterId)
            throw new AccountNotOwnedByCustomerException(AccountId, requesterId);
    }

    // ── 業務規則 2：帳戶狀態驗證 ────────────────────────────────────────
    public void EnsureActive()
    {
        if (Status != AccountStatus.Active)
            throw new AccountNotActiveException(AccountId, Status);
    }

    // ── 業務規則 3：查詢區間限制 + 過濾 ────────────────────────────────
    // Application Layer 取得原始交易後傳入，Domain 執行業務規則。
    public TransactionHistory FilterByDateRange(
        IReadOnlyList<Transaction> transactions,
        DateRange dateRange)
    {
        if (dateRange.ExceedsMonths(13))
            throw new QueryRangeExceededException("查詢區間不可超過 13 個月");

        var filtered = transactions
            .Where(t => dateRange.Contains(DateOnly.FromDateTime(t.TransactionDate)))
            .OrderByDescending(t => t.TransactionDate)
            .ToList()
            .AsReadOnly();

        return new TransactionHistory(AccountId, filtered, dateRange);
    }

    public bool IsOwnedBy(CustomerId customerId) => OwnerId == customerId;
}
