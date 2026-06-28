using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// 範例資料種子（供開發 / 測試使用，對應 BDD 情境）。
/// </summary>
public static class DatabaseSeeder
{
    public static void Seed(BankDbContext db, bool eventSourcedPrivileges = false)
    {
        if (db.Accounts.Any()) return;

        // ── 帳戶 ────────────────────────────────────────────────────────
        db.Accounts.AddRange(
            new AccountEntity
            {
                AccountId = "00123456789012", OwnerId = "C001",
                AccountType = AccountType.Twd, Currency = Currency.TWD,
                Status = AccountStatus.Active
            },
            new AccountEntity
            {
                AccountId = "00123456789099", OwnerId = "C001",
                AccountType = AccountType.Fx, Currency = Currency.USD,
                Status = AccountStatus.Active
            },
            new AccountEntity
            {
                AccountId = "00123456780000", OwnerId = "C001",
                AccountType = AccountType.Twd, Currency = Currency.TWD,
                Status = AccountStatus.Frozen
            },
            new AccountEntity
            {
                AccountId = "00999999999999", OwnerId = "C999",
                AccountType = AccountType.Twd, Currency = Currency.TWD,
                Status = AccountStatus.Active
            });

        // ── 台幣交易（帳戶 00123456789012）─────────────────────────────
        db.Transactions.AddRange(
            Twd("T-TWD-001", "00123456789012", TransactionType.Credit, 50000m,
                new DateTime(2025, 1, 5), "薪資轉帳", TransactionChannel.AutoTransfer),
            Twd("T-TWD-002", "00123456789012", TransactionType.Debit, 10000m,
                new DateTime(2025, 1, 10), "ATM 提款", TransactionChannel.Atm),
            Twd("T-TWD-003", "00123456789012", TransactionType.Credit, 3000m,
                new DateTime(2025, 1, 20), "利息入帳", TransactionChannel.AutoTransfer));

        // ── 外幣交易（帳戶 00123456789099，USD）─────────────────────────
        db.Transactions.AddRange(
            Fx("T-FX-001", "00123456789099", TransactionType.Credit, 1000m, 31500m,
                new DateTime(2025, 1, 8), "USD 匯入", TransactionChannel.NetBanking),
            Fx("T-FX-002", "00123456789099", TransactionType.Debit, 200m, 6320m,
                new DateTime(2025, 1, 15), "USD 換匯", TransactionChannel.MobileApp));

        // ── 轉帳優惠（依設定：狀態儲存或事件溯源）────────────────────────
        if (eventSourcedPrivileges)
        {
            SeedEventSourcedPrivileges(db);
        }
        else
        {
        db.Privileges.Add(new PrivilegeEntity
        {
            PrivilegeId = "P001", OwnerId = "C001",
            Type = PrivilegeType.FeeWaiverInterBank,
            TotalQuota = 10, UsedQuota = 3,
            ValidFrom = new DateOnly(2025, 1, 1),
            ValidTo = new DateOnly(2025, 12, 31),
            UsageRecords = new List<PrivilegeUsageEntity>
            {
                new() { UsageId = "U001", PrivilegeId = "P001",
                        UsedDate = new DateOnly(2025, 1, 12), SavedAmount = 15m,
                        Currency = Currency.TWD, Description = "跨行轉帳免手續費" },
                new() { UsageId = "U002", PrivilegeId = "P001",
                        UsedDate = new DateOnly(2025, 2, 3), SavedAmount = 15m,
                        Currency = Currency.TWD, Description = "跨行轉帳免手續費" },
                new() { UsageId = "U003", PrivilegeId = "P001",
                        UsedDate = new DateOnly(2025, 3, 9), SavedAmount = 30m,
                        Currency = Currency.TWD, Description = "跨行轉帳免手續費" }
            }
        });

        // 屬於其他客戶（C999）的優惠 — 用於越權測試
        db.Privileges.Add(new PrivilegeEntity
        {
            PrivilegeId = "P999", OwnerId = "C999",
            Type = PrivilegeType.FeeWaiverCrossBorder,
            TotalQuota = 5, UsedQuota = 0,
            ValidFrom = new DateOnly(2025, 1, 1),
            ValidTo = new DateOnly(2025, 12, 31)
        });

        // ── 寫入側（使用優惠）專用優惠，掛在客戶 C002 名下，
        //    避免影響 C001 的讀取情境（有效期間刻意涵蓋未來以確保可用）──
        db.Privileges.AddRange(
            // 可正常使用：總 5 已用 0
            new PrivilegeEntity
            {
                PrivilegeId = "P010", OwnerId = "C002",
                Type = PrivilegeType.FeeWaiverInterBank,
                TotalQuota = 5, UsedQuota = 0,
                ValidFrom = new DateOnly(2025, 1, 1),
                ValidTo = new DateOnly(2099, 12, 31)
            },
            // 已用盡：總 1 已用 1
            new PrivilegeEntity
            {
                PrivilegeId = "P012", OwnerId = "C002",
                Type = PrivilegeType.FeeWaiverInterBank,
                TotalQuota = 1, UsedQuota = 1,
                ValidFrom = new DateOnly(2025, 1, 1),
                ValidTo = new DateOnly(2099, 12, 31)
            },
            // 已過期：有效期間在過去
            new PrivilegeEntity
            {
                PrivilegeId = "P013", OwnerId = "C002",
                Type = PrivilegeType.FeeWaiverInterBank,
                TotalQuota = 5, UsedQuota = 0,
                ValidFrom = new DateOnly(2020, 1, 1),
                ValidTo = new DateOnly(2020, 12, 31)
            });
        }

        db.SaveChanges();
    }

    // ── 事件溯源版的優惠種子：以 Grant + Use 產生事件串流（同等可觀察狀態）──
    private static void SeedEventSourcedPrivileges(BankDbContext db)
    {
        // P001：核發後使用 3 次（剩餘 7）
        var p001 = TransferPrivilege.Grant(
            PrivilegeId.Of("P001"), CustomerId.Of("C001"),
            PrivilegeType.FeeWaiverInterBank, 10,
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)));
        p001.Use("U001", Money.Twd(15m), "跨行轉帳免手續費", new DateOnly(2025, 1, 12));
        p001.Use("U002", Money.Twd(15m), "跨行轉帳免手續費", new DateOnly(2025, 2, 3));
        p001.Use("U003", Money.Twd(30m), "跨行轉帳免手續費", new DateOnly(2025, 3, 9));
        AppendStream(db, p001);

        // P999：他人（C999）的優惠 — 僅核發
        AppendStream(db, TransferPrivilege.Grant(
            PrivilegeId.Of("P999"), CustomerId.Of("C999"),
            PrivilegeType.FeeWaiverCrossBorder, 5,
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))));

        // P010：可正常使用（C002）
        AppendStream(db, TransferPrivilege.Grant(
            PrivilegeId.Of("P010"), CustomerId.Of("C002"),
            PrivilegeType.FeeWaiverInterBank, 5,
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2099, 12, 31))));

        // P012：核發後用盡（剩餘 0）
        var p012 = TransferPrivilege.Grant(
            PrivilegeId.Of("P012"), CustomerId.Of("C002"),
            PrivilegeType.FeeWaiverInterBank, 1,
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2099, 12, 31)));
        p012.Use("U012", Money.Twd(15m), "跨行轉帳免手續費", DateOnly.FromDateTime(DateTime.UtcNow));
        AppendStream(db, p012);

        // P013：已過期（C002）— 僅核發
        AppendStream(db, TransferPrivilege.Grant(
            PrivilegeId.Of("P013"), CustomerId.Of("C002"),
            PrivilegeType.FeeWaiverInterBank, 5,
            new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31))));
    }

    private static void AppendStream(BankDbContext db, TransferPrivilege aggregate)
    {
        long version = 0;
        foreach (var domainEvent in aggregate.DomainEvents)
        {
            var (type, payload) = Events.DomainEventSerialization.Serialize(domainEvent);
            db.PrivilegeEvents.Add(new PrivilegeEventEntity
            {
                StreamId = aggregate.PrivilegeId.Value,
                Version = ++version,
                Type = type,
                Payload = payload,
                OccurredOnUtc = domainEvent.OccurredOn
            });
        }
    }

    private static TransactionEntity Twd(
        string id, string accountId, TransactionType type, decimal amount,
        DateTime date, string desc, TransactionChannel channel) => new()
    {
        TransactionId = id, AccountId = accountId, Type = type,
        Amount = amount, Currency = Currency.TWD, TwdEquivalent = null,
        TransactionDate = date, Description = desc, Channel = channel
    };

    private static TransactionEntity Fx(
        string id, string accountId, TransactionType type, decimal amount,
        decimal twdEquivalent, DateTime date, string desc,
        TransactionChannel channel) => new()
    {
        TransactionId = id, AccountId = accountId, Type = type,
        Amount = amount, Currency = Currency.USD, TwdEquivalent = twdEquivalent,
        TransactionDate = date, Description = desc, Channel = channel
    };
}
