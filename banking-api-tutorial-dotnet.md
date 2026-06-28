# 銀行帳戶查詢 API — 架構規劃 Tutorial (.NET Core + MediatR)

> **技術棧**：.NET 9 · ASP.NET Core · MediatR · DDD 戰術設計 · Hexagonal Architecture · SOLID · CQRS · TDD · BDD
> **業務範圍**：台幣/外幣活存交易紀錄查詢、轉帳優惠查詢與使用紀錄查詢
> **設計說明**：MediatR 作為 CQRS 的 In-Process Message Bus，Query / QueryHandler 透過 MediatR Pipeline 解耦，業務規則封裝於 Domain Aggregate，Repository Interface（Output Port）定義於 Application Layer，Domain Layer 保持零外部依賴。

---

## 目錄

1. [業務需求說明](#1-業務需求說明)
2. [架構核心原則宣告](#2-架構核心原則宣告)
3. [系統架構概覽](#3-系統架構概覽)
4. [Hexagonal Architecture 分層設計](#4-hexagonal-architecture-分層設計)
5. [MediatR 在 CQRS 的角色](#5-mediatr-在-cqrs-的角色)
6. [DDD 戰術設計](#6-ddd-戰術設計)
7. [CQRS 設計（MediatR 實作）](#7-cqrs-設計mediatr-實作)
8. [MediatR Pipeline Behaviors](#8-mediatr-pipeline-behaviors)
9. [API 設計規範](#9-api-設計規範)
10. [TDD 設計規劃](#10-tdd-設計規劃)
11. [BDD 設計規劃](#11-bdd-設計規劃)
12. [專案結構](#12-專案結構)
13. [工項清單 Work Breakdown Structure](#13-工項清單-work-breakdown-structure)
14. [技術選型說明](#14-技術選型說明)
15. [非功能性需求考量](#15-非功能性需求考量)
16. [附錄：SOLID 原則對應表](#16-附錄solid-原則對應表)
17. [ADR：Repository Pattern 設計決策](#17-adrrepository-pattern-設計決策)

---

## 1. 業務需求說明

### 1.1 使用者情境

| 功能 | 說明 | 對應角色 |
|------|------|----------|
| 台幣活存交易紀錄查詢 | 客戶可依日期區間查詢台幣帳戶進出交易 | 已認證銀行客戶 |
| 外幣活存交易紀錄查詢 | 客戶可依幣別與日期區間查詢外幣帳戶交易 | 已認證銀行客戶 |
| 轉帳優惠內容查詢 | 查詢目前可用之轉帳優惠方案（例如免手續費次數） | 已認證銀行客戶 |
| 轉帳優惠使用紀錄查詢 | 查詢客戶已使用的轉帳優惠歷史紀錄 | 已認證銀行客戶 |

### 1.2 核心業務規則

- 客戶只能查詢自己名下的帳戶資料（**所有權驗證封裝於 Aggregate 內部**）
- 外幣交易紀錄需呈現原幣金額與台幣等值金額
- 查詢區間不得超過 13 個月（**由 Aggregate 的 Domain Method 強制執行**）
- 轉帳優惠有效期限與使用上限需即時反映
- 所有查詢操作需留存稽核日誌（**由 MediatR Pipeline Behavior 統一處理**）

---

## 2. 架構核心原則宣告

### 2.1 依賴方向（Dependency Rule）

```
Infrastructure Layer  →  Application Layer  →  Domain Layer
   (最外層)                  (協調層)             (最內層，純粹)

規則：箭頭方向 = 「依賴」方向
      內層永遠不知道外層的存在
      Domain Layer 不依賴任何人，包含 MediatR
```

### 2.2 各層對外部的感知邊界

| 層次 | 可依賴 | 絕對不可依賴 |
|------|--------|------------|
| **Domain Layer** | 自身 Model / Value Object / Exception | Application、Infrastructure、MediatR、EF Core、任何 NuGet Package |
| **Application Layer** | Domain Layer、MediatR Interfaces（`IRequest`、`IRequestHandler`）、自定義 Port Interfaces | Infrastructure 實作類別 |
| **Infrastructure Layer** | Application Layer Ports、Domain Layer、ASP.NET Core、EF Core、StackExchange.Redis 等 | 無限制 |

### 2.3 MediatR 的正確定位

```
MediatR 是 Application Layer 的工具，不是 Domain Layer 的工具。

✅ Query / QueryHandler 定義於 Application Layer，實作 IRequest / IRequestHandler
✅ Pipeline Behavior（Logging、Validation、AuditLog）定義於 Application Layer
✅ Controller 透過 ISender 發送 Query，不直接相依 Handler
❌ Domain Model 不可實作任何 MediatR Interface
❌ Domain Event 不直接使用 MediatR INotification（需透過 Domain Event Dispatcher）
```

### 2.4 Repository Interface 的正確歸屬

```
❌ 錯誤：Repository Interface 屬於 Domain Layer
✅ 正確：Repository Interface（Output Port）屬於 Application Layer

理由：
  定義「需要什麼資料」的是 Application Layer（Query Handler）。
  Domain Layer 只定義 Model 本身，對資料如何取得一無所知。
  Output Port Interface 定義於 Application/Ports/Out/，
  由 Infrastructure Layer 的 Adapter 實作。
```

---

## 3. 系統架構概覽

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Layer                             │
│              Mobile App / Web App / Third-party                 │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTPS / JWT
┌───────────────────────────▼─────────────────────────────────────┐
│                    API Gateway / BFF                             │
│              Rate Limiting · Auth Verification                  │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│              Banking Account Query Service (.NET 9)             │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │   Infrastructure — Driving Adapters (Inbound)            │  │
│  │         ASP.NET Core Minimal API / Controllers           │  │
│  └──────────────────┬───────────────────────────────────────┘  │
│                     │ ISender.Send(query)                       │
│  ┌──────────────────▼───────────────────────────────────────┐  │
│  │              MediatR Pipeline                            │  │
│  │   LoggingBehavior → ValidationBehavior → AuditBehavior  │  │
│  └──────────────────┬───────────────────────────────────────┘  │
│                     │                                           │
│  ┌──────────────────▼───────────────────────────────────────┐  │
│  │   Application Layer — Query Handlers                     │  │
│  │   IRequestHandler<TQuery, TResult>                       │  │
│  │   協調流程：呼叫 Output Port → 委派 Domain 業務規則       │  │
│  └──────────┬─────────────────────────┬─────────────────────┘  │
│             │ Output Port Interface    │ Domain Objects          │
│  ┌──────────▼──────────┐   ┌──────────▼──────────────────┐     │
│  │  Infrastructure     │   │  Domain Layer（純粹）        │     │
│  │  Driven Adapters    │   │  Aggregates / Value Objects  │     │
│  │  EF Core / Redis /  │   │  Domain Exceptions           │     │
│  │  Core Banking HTTP  │   │  零 NuGet 依賴               │     │
│  └─────────────────────┘   └─────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
   PostgreSQL /        Core Banking          Redis Cache
   Read DB             System (IBM)
```

---

## 4. Hexagonal Architecture 分層設計

### 4.1 Port & Adapter 對應關係

```
REST Client ──► [Input Port]  ──►  Application  ──►  [Output Port]  ──►  DB
               IRequest<T>         MediatR            ILoadAccountPort    EF Core
               (MediatR Query)     Handler            (定義於 App Layer)  Adapter
                    │                                       │
             Driving Adapter                         Driven Adapter
          (Controller 呼叫                        (EF Core / Redis
           ISender.Send())                         實作 Output Port)
```

### 4.2 各層職責說明

#### 4.2.1 Domain Layer（純粹核心）

```
BankAccountQuery.Domain/
├── Model/
│   ├── Account/          # Account Aggregate
│   ├── Privilege/        # TransferPrivilege Aggregate
│   └── Shared/           # 共用 Value Objects
└── Exceptions/           # Domain Exceptions（業務語意）
```

**設計約束**：
- `.csproj` 中**不引入任何 NuGet Package**
- 不定義 Repository Interface
- 所有業務規則封裝於 Aggregate Method

#### 4.2.2 Application Layer（Use Cases + Port 定義 + MediatR）

```
BankAccountQuery.Application/
├── Ports/
│   ├── In/               # 由 MediatR IRequest 取代（不需另外定義 Interface）
│   └── Out/              # Output Port Interfaces（由 Adapter 實作）
├── Queries/
│   ├── Account/          # Query Records + Handlers + Result DTOs
│   └── Privilege/        # Query Records + Handlers + Result DTOs
└── Behaviors/            # MediatR Pipeline Behaviors
```

**設計約束**：
- Handler 只做協調：呼叫 Output Port → 委派 Domain Method → 轉換 DTO
- Pipeline Behavior 處理橫切關注點（Logging、Validation、Audit Log）

#### 4.2.3 Infrastructure Layer（Adapters）

```
BankAccountQuery.Infrastructure/
├── Adapters/
│   ├── In/Web/           # Driving Adapters（ASP.NET Core Controllers / Minimal API）
│   └── Out/
│       ├── Persistence/  # Driven Adapters（EF Core，實作 Output Port）
│       ├── CoreBanking/  # Driven Adapters（HTTP Client，實作 Output Port）
│       └── Cache/        # Driven Adapters（Redis，實作 Output Port）
└── Configuration/        # DI 註冊、EF Core DbContext、FluentValidation 設定
```

---

## 5. MediatR 在 CQRS 的角色

### 5.1 MediatR 核心概念

```
MediatR 實作 Mediator Pattern，作為 In-Process Message Bus：

  Sender（Controller）
      │
      │ ISender.Send(query)
      ▼
  MediatR（Message Dispatcher）
      │
      │ 根據 TRequest 型別路由至對應 Handler
      ▼
  IRequestHandler<TQuery, TResult>（Query Handler）

優點：
  ✅ Controller 與 Handler 完全解耦，互不知道彼此
  ✅ Pipeline Behavior 統一處理橫切關注點，Handler 保持純粹
  ✅ CQRS 的 Query / Command 分離天然對應 IRequest<TResult>
  ✅ 測試 Handler 不需啟動 ASP.NET Core Host
```

### 5.2 MediatR 與 Hexagonal Architecture 的對應

```
傳統 Hexagonal         MediatR 版本
─────────────────────────────────────────────────
Input Port Interface  →  IRequest<TResult>（MediatR 內建）
Input Port 實作       →  IRequestHandler<TQuery, TResult>
Controller 呼叫方式   →  ISender.Send(new GetTwdTransactionHistoryQuery(...))
Pipeline / 橫切       →  IPipelineBehavior<TRequest, TResponse>
```

### 5.3 Query 與 Command 分離

```csharp
// Query（Read Side）— 實作 IRequest<TResult>，不改變系統狀態
public record GetTwdTransactionHistoryQuery(...) : IRequest<TwdTransactionHistoryResult>;

// Command（Write Side）— 本 Tutorial 不涵蓋，示意如下
// public record TransferCommand(...) : IRequest<TransferResult>;

// Handler 一對一對應 Query
public class GetTwdTransactionHistoryHandler
    : IRequestHandler<GetTwdTransactionHistoryQuery, TwdTransactionHistoryResult>
{
    public async Task<TwdTransactionHistoryResult> Handle(
        GetTwdTransactionHistoryQuery query,
        CancellationToken cancellationToken) { ... }
}
```

---

## 6. DDD 戰術設計

### 6.1 Bounded Context 劃分

```
┌──────────────────────────┐    ┌──────────────────────────┐
│    Account Context       │    │   Privilege Context      │
│                          │    │                          │
│  Account (Aggregate)     │    │  TransferPrivilege       │
│  Transaction (Entity)    │    │  (Aggregate)             │
│  Money (Value Object)    │    │  PrivilegeUsageRecord    │
│  DateRange (VO)          │    │  (Entity)                │
│  Currency (VO)           │    │  PrivilegeType (Enum)    │
└──────────────────────────┘    └──────────────────────────┘
              ▲                               ▲
              └──────────── Shared ──────────┘
                     CustomerId (Value Object)
                     AccountId  (Value Object)
                     Money      (Value Object)
                     DateRange  (Value Object)
```

### 6.2 Shared Value Objects（Domain/Model/Shared/）

```csharp
// Money — 不可變，封裝金額與幣別業務語意
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("金額不可為負數", nameof(amount));
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("金額最多 2 位小數", nameof(amount));
        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
        return new Money(Amount + other.Amount, Currency);
    }

    public static Money Twd(decimal amount) => new(amount, Currency.TWD);

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => Equals(obj as Money);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
}

// DateRange — 封裝日期區間驗證與業務語意
public sealed record DateRange(DateOnly StartDate, DateOnly EndDate)
{
    public DateRange
    {
        if (StartDate > EndDate)
            throw new ArgumentException("StartDate 不可晚於 EndDate");
    }

    public bool ExceedsMonths(int months) =>
        ((EndDate.Year - StartDate.Year) * 12 + EndDate.Month - StartDate.Month) > months;

    public bool Contains(DateOnly date) =>
        date >= StartDate && date <= EndDate;
}

// CustomerId — 強型別，防止 Primitive Obsession
public sealed record CustomerId(string Value)
{
    public CustomerId
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("CustomerId 不可為空");
    }

    public static CustomerId Of(string value) => new(value);
}

// AccountId — 封裝帳號格式驗證
public sealed record AccountId(string Value)
{
    public AccountId
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(Value, @"^\d{14}$"))
            throw new InvalidAccountIdFormatException("帳號格式不正確，需為 14 位數字");
    }
}
```

### 6.3 Account Aggregate（Domain/Model/Account/）

```csharp
// Account — Aggregate Root
// 業務規則完全封裝於此，Application Layer 只協調，不判斷
public sealed class Account
{
    public AccountId AccountId { get; }
    public CustomerId OwnerId { get; }
    public AccountType AccountType { get; }       // TWD / FX
    public Currency Currency { get; }
    public AccountStatus Status { get; private set; }  // ACTIVE / FROZEN / CLOSED

    private Account() { } // EF Core 用

    public Account(AccountId accountId, CustomerId ownerId,
                   AccountType accountType, Currency currency,
                   AccountStatus status)
    {
        AccountId = accountId;
        OwnerId = ownerId;
        AccountType = accountType;
        Currency = currency;
        Status = status;
    }

    // ── 業務規則 1：所有權驗證 ─────────────────────────────────────
    public void VerifyOwnership(CustomerId requesterId)
    {
        if (OwnerId != requesterId)
            throw new AccountNotOwnedByCustomerException(AccountId, requesterId);
    }

    // ── 業務規則 2：帳戶狀態驗證 ──────────────────────────────────
    public void EnsureActive()
    {
        if (Status != AccountStatus.Active)
            throw new AccountNotActiveException(AccountId, Status);
    }

    // ── 業務規則 3：查詢區間限制 + 過濾 ───────────────────────────
    // Application Layer 取得原始交易後傳入，Domain 執行業務規則
    public TransactionHistory FilterByDateRange(
        IReadOnlyList<Transaction> transactions,
        DateRange dateRange)
    {
        if (dateRange.ExceedsMonths(13))
            throw new QueryRangeExceededException("查詢區間不可超過 13 個月");

        var filtered = transactions
            .Where(t => dateRange.Contains(DateOnly.FromDateTime(t.TransactionDate)))
            .ToList()
            .AsReadOnly();

        return new TransactionHistory(AccountId, filtered, dateRange);
    }

    public bool IsOwnedBy(CustomerId customerId) => OwnerId == customerId;
}

// Transaction — Entity（屬於 Account Aggregate 邊界內）
public sealed class Transaction
{
    public TransactionId TransactionId { get; }
    public TransactionType Type { get; }           // Credit / Debit
    public Money Amount { get; }                   // 原幣金額
    public Money? TwdEquivalent { get; }           // 台幣等值（外幣才有）
    public DateTime TransactionDate { get; }
    public string Description { get; }
    public TransactionChannel Channel { get; }

    public Transaction(TransactionId transactionId, TransactionType type,
                       Money amount, Money? twdEquivalent,
                       DateTime transactionDate, string description,
                       TransactionChannel channel)
    {
        TransactionId = transactionId;
        Type = type;
        Amount = amount;
        TwdEquivalent = twdEquivalent;
        TransactionDate = transactionDate;
        Description = description;
        Channel = channel;
    }
}

// TransactionHistory — Value Object（查詢結果封裝）
public sealed record TransactionHistory(
    AccountId AccountId,
    IReadOnlyList<Transaction> Transactions,
    DateRange QueriedRange)
{
    public int Count => Transactions.Count;
}
```

### 6.4 TransferPrivilege Aggregate（Domain/Model/Privilege/）

```csharp
public sealed class TransferPrivilege
{
    public PrivilegeId PrivilegeId { get; }
    public CustomerId OwnerId { get; }
    public PrivilegeType Type { get; }
    public int TotalQuota { get; }
    public int UsedQuota { get; }
    public DateRange ValidPeriod { get; }
    private readonly List<PrivilegeUsageRecord> _usageRecords;

    // ── 業務規則 1：優惠是否有效 ───────────────────────────────────
    public bool IsValid() => IsWithinValidPeriod() && HasRemainingQuota();

    private bool IsWithinValidPeriod() =>
        ValidPeriod.Contains(DateOnly.FromDateTime(DateTime.Today));

    private bool HasRemainingQuota() => GetRemainingQuota() > 0;

    // ── 業務規則 2：剩餘次數 ───────────────────────────────────────
    public int GetRemainingQuota() => TotalQuota - UsedQuota;

    // ── 業務規則 3：所有權驗證 ────────────────────────────────────
    public void VerifyOwnership(CustomerId requesterId)
    {
        if (OwnerId != requesterId)
            throw new PrivilegeNotOwnedByCustomerException(PrivilegeId, requesterId);
    }

    // ── 業務規則 4：使用紀錄過濾 ─────────────────────────────────
    public PrivilegeUsageHistory FilterUsageHistory(DateRange dateRange)
    {
        var filtered = _usageRecords
            .Where(r => dateRange.Contains(r.UsedDate))
            .ToList()
            .AsReadOnly();

        return new PrivilegeUsageHistory(PrivilegeId, filtered, dateRange);
    }
}
```

### 6.5 Domain Exceptions（Domain/Exceptions/）

```csharp
// 所有 Exception 使用業務語意命名，不含技術細節
public sealed class AccountNotOwnedByCustomerException : DomainException
{
    public AccountNotOwnedByCustomerException(AccountId accountId, CustomerId customerId)
        : base($"帳戶 [{accountId.Value}] 不屬於客戶 [{customerId.Value}]") { }
}

public sealed class QueryRangeExceededException : DomainException
{
    public QueryRangeExceededException(string message) : base(message) { }
}

public sealed class AccountNotActiveException : DomainException
{
    public AccountNotActiveException(AccountId accountId, AccountStatus status)
        : base($"帳戶 [{accountId.Value}] 狀態為 [{status}]，無法查詢") { }
}

// 所有 Domain Exception 的基底類別
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
```

---

## 7. CQRS 設計（MediatR 實作）

### 7.1 Output Port Interfaces（Application/Ports/Out/）

```csharp
// ── 帳戶 Output Ports ─────────────────────────────────────────────

public interface ILoadAccountPort
{
    Task<Account?> FindByAccountIdAsync(
        AccountId accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> FindAllByCustomerIdAsync(
        CustomerId customerId,
        CancellationToken cancellationToken = default);
}

public interface ILoadTransactionPort
{
    Task<IReadOnlyList<Transaction>> FindByAccountIdAsync(
        AccountId accountId,
        DateRange dateRange,
        CancellationToken cancellationToken = default);
}

// ── 優惠 Output Ports ─────────────────────────────────────────────

public interface ILoadPrivilegePort
{
    Task<IReadOnlyList<TransferPrivilege>> FindByCustomerIdAsync(
        CustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<TransferPrivilege?> FindByPrivilegeIdAsync(
        PrivilegeId privilegeId,
        CancellationToken cancellationToken = default);
}
```

> **命名說明**：Port 以 `ILoad` / `ISave` 為前綴，表達 Application Layer 的**意圖**，
> 不使用 `IRepository` 字眼，避免暗示特定持久化技術，
> 且支援 EF Core / Redis / HTTP 等多種實作互換。

### 7.2 Query Records（Application/Queries/）

```csharp
// 所有 Query 使用 C# record（不可變），實作 IRequest<TResult>

public sealed record GetTwdTransactionHistoryQuery(
    CustomerId CustomerId,
    AccountId AccountId,
    DateRange DateRange,
    int Page,
    int Size
) : IRequest<TwdTransactionHistoryResult>;

public sealed record GetFxTransactionHistoryQuery(
    CustomerId CustomerId,
    AccountId AccountId,
    Currency Currency,
    DateRange DateRange,
    int Page,
    int Size
) : IRequest<FxTransactionHistoryResult>;

public sealed record GetTransferPrivilegeQuery(
    CustomerId CustomerId
) : IRequest<TransferPrivilegeResult>;

public sealed record GetPrivilegeUsageHistoryQuery(
    CustomerId CustomerId,
    PrivilegeId PrivilegeId,
    DateRange DateRange,
    int Page,
    int Size
) : IRequest<PrivilegeUsageHistoryResult>;
```

### 7.3 Query Handlers（Application/Queries/Handlers/）

```csharp
// ── GetTwdTransactionHistoryHandler ──────────────────────────────
public sealed class GetTwdTransactionHistoryHandler
    : IRequestHandler<GetTwdTransactionHistoryQuery, TwdTransactionHistoryResult>
{
    private readonly ILoadAccountPort _loadAccountPort;
    private readonly ILoadTransactionPort _loadTransactionPort;

    public GetTwdTransactionHistoryHandler(
        ILoadAccountPort loadAccountPort,
        ILoadTransactionPort loadTransactionPort)
    {
        _loadAccountPort = loadAccountPort;
        _loadTransactionPort = loadTransactionPort;
    }

    public async Task<TwdTransactionHistoryResult> Handle(
        GetTwdTransactionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        // Step 1：透過 Output Port 取得 Aggregate
        var account = await _loadAccountPort.FindByAccountIdAsync(
            query.AccountId, cancellationToken)
            ?? throw new AccountNotFoundException(query.AccountId);

        // Step 2：委派業務規則至 Domain Model
        account.VerifyOwnership(query.CustomerId);   // Domain 執行
        account.EnsureActive();                       // Domain 執行

        // Step 3：透過 Output Port 取得原始交易資料
        var rawTransactions = await _loadTransactionPort.FindByAccountIdAsync(
            query.AccountId, query.DateRange, cancellationToken);

        // Step 4：委派業務規則至 Domain Model（區間限制 + 過濾）
        var history = account.FilterByDateRange(rawTransactions, query.DateRange);

        // Step 5：轉換為 Read Model（Application Layer 職責）
        return TwdTransactionHistoryResult.From(history, query.Page, query.Size);
    }
}

// ── GetFxTransactionHistoryHandler ───────────────────────────────
public sealed class GetFxTransactionHistoryHandler
    : IRequestHandler<GetFxTransactionHistoryQuery, FxTransactionHistoryResult>
{
    private readonly ILoadAccountPort _loadAccountPort;
    private readonly ILoadTransactionPort _loadTransactionPort;

    public async Task<FxTransactionHistoryResult> Handle(
        GetFxTransactionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var account = await _loadAccountPort.FindByAccountIdAsync(
            query.AccountId, cancellationToken)
            ?? throw new AccountNotFoundException(query.AccountId);

        account.VerifyOwnership(query.CustomerId);
        account.EnsureActive();

        var rawTransactions = await _loadTransactionPort.FindByAccountIdAsync(
            query.AccountId, query.DateRange, cancellationToken);

        var history = account.FilterByDateRange(rawTransactions, query.DateRange);

        return FxTransactionHistoryResult.From(history, query.Currency,
                                               query.Page, query.Size);
    }
}

// ── GetTransferPrivilegeHandler ───────────────────────────────────
public sealed class GetTransferPrivilegeHandler
    : IRequestHandler<GetTransferPrivilegeQuery, TransferPrivilegeResult>
{
    private readonly ILoadPrivilegePort _loadPrivilegePort;

    public async Task<TransferPrivilegeResult> Handle(
        GetTransferPrivilegeQuery query,
        CancellationToken cancellationToken)
    {
        var privileges = await _loadPrivilegePort.FindByCustomerIdAsync(
            query.CustomerId, cancellationToken);

        // 每個 Aggregate 自己計算業務狀態（IsValid、GetRemainingQuota）
        return TransferPrivilegeResult.From(privileges);
    }
}

// ── GetPrivilegeUsageHistoryHandler ──────────────────────────────
public sealed class GetPrivilegeUsageHistoryHandler
    : IRequestHandler<GetPrivilegeUsageHistoryQuery, PrivilegeUsageHistoryResult>
{
    private readonly ILoadPrivilegePort _loadPrivilegePort;

    public async Task<PrivilegeUsageHistoryResult> Handle(
        GetPrivilegeUsageHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var privilege = await _loadPrivilegePort.FindByPrivilegeIdAsync(
            query.PrivilegeId, cancellationToken)
            ?? throw new PrivilegeNotFoundException(query.PrivilegeId);

        privilege.VerifyOwnership(query.CustomerId);  // Domain 執行

        var usageHistory = privilege.FilterUsageHistory(query.DateRange); // Domain 執行

        return PrivilegeUsageHistoryResult.From(usageHistory, query.Page, query.Size);
    }
}
```

### 7.4 Read Models（Application/Queries/Results/）

```csharp
// 台幣交易紀錄 Read Model
public sealed record TwdTransactionHistoryResult(
    string AccountId,
    IReadOnlyList<TwdTransactionDto> Transactions,
    PageInfo PageInfo)
{
    public static TwdTransactionHistoryResult From(
        TransactionHistory history, int page, int size)
    {
        var dtos = history.Transactions
            .Select(TwdTransactionDto.From)
            .ToList();
        return new TwdTransactionHistoryResult(
            history.AccountId.Value,
            Paginate(dtos, page, size),
            PageInfo.Of(page, size, dtos.Count));
    }
}

public sealed record TwdTransactionDto(
    string TransactionId,
    string TransactionDate,
    string TransactionType,
    string Amount,
    string Description,
    string Channel)
{
    public static TwdTransactionDto From(Transaction t) => new(
        t.TransactionId.Value,
        t.TransactionDate.ToString("yyyy-MM-dd"),
        t.Type.ToString(),
        t.Amount.Amount.ToString("N2"),
        t.Description,
        t.Channel.ToString());
}

// 外幣交易紀錄 Read Model
public sealed record FxTransactionDto(
    string TransactionId,
    string TransactionDate,
    string TransactionType,
    string CurrencyCode,
    string FxAmount,
    string TwdEquivalent,
    string ExchangeRate,
    string Description);

// 優惠方案 Read Model（從 Domain Aggregate 轉換，讀取計算後的值）
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
```

---

## 8. MediatR Pipeline Behaviors

Pipeline Behavior 是 MediatR 的攔截機制，對應 AOP 的概念。
所有 Query 在進入 Handler 前，會依序通過已註冊的 Behavior。
這是處理橫切關注點（Logging、Validation、Audit Log）的正確位置，
**Handler 本身保持純粹，只負責業務流程協調**。

```
ISender.Send(query)
    │
    ▼
LoggingBehavior         ← 記錄 Query 開始 / 結束 / 耗時
    │
    ▼
ValidationBehavior      ← FluentValidation 驗證 Query 參數
    │
    ▼
AuditLogBehavior        ← 寫入稽核日誌（CustomerId、QueryType、Timestamp）
    │
    ▼
Handler.Handle()        ← 純粹的業務流程協調
```

### 8.1 LoggingBehavior

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var queryName = typeof(TRequest).Name;
        _logger.LogInformation("處理 Query：{QueryName} {@Query}", queryName, request);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation("完成 Query：{QueryName}，耗時 {ElapsedMs}ms",
                queryName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query 失敗：{QueryName}，耗時 {ElapsedMs}ms",
                queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 8.2 ValidationBehavior（整合 FluentValidation）

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new QueryValidationException(failures);

        return await next();
    }
}

// Query Validator（對應 FluentValidation）
public sealed class GetTwdTransactionHistoryQueryValidator
    : AbstractValidator<GetTwdTransactionHistoryQuery>
{
    public GetTwdTransactionHistoryQueryValidator()
    {
        RuleFor(q => q.AccountId)
            .NotNull().WithMessage("帳號不可為空");

        RuleFor(q => q.DateRange)
            .NotNull().WithMessage("查詢區間不可為空");

        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(0).WithMessage("頁碼不可為負數");

        RuleFor(q => q.Size)
            .InclusiveBetween(1, 100).WithMessage("每頁筆數需介於 1 至 100");
    }
}
```

### 8.3 AuditLogBehavior

```csharp
public sealed class AuditLogBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditLogPort _auditLogPort;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // 只記錄有 CustomerId 屬性的 Query（反射或 Interface 萃取）
        if (request is ICustomerQuery customerQuery)
        {
            await _auditLogPort.RecordAsync(new AuditLogEntry(
                CustomerId: customerQuery.CustomerId.Value,
                QueryType: typeof(TRequest).Name,
                IpAddress: _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                Timestamp: DateTime.UtcNow
            ), cancellationToken);
        }

        return response;
    }
}

// 所有含 CustomerId 的 Query 實作此 Interface（讓 AuditLogBehavior 識別）
public interface ICustomerQuery
{
    CustomerId CustomerId { get; }
}

// 更新 Query Record 實作此 Interface
public sealed record GetTwdTransactionHistoryQuery(
    CustomerId CustomerId,
    AccountId AccountId,
    DateRange DateRange,
    int Page,
    int Size
) : IRequest<TwdTransactionHistoryResult>, ICustomerQuery;
```

### 8.4 DI 註冊（Infrastructure/Configuration/）

```csharp
// Program.cs 或 ServiceCollectionExtensions.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetTwdTransactionHistoryHandler).Assembly);

    // Pipeline Behavior 依序執行（順序即優先級）
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(GetTwdTransactionHistoryQueryValidator).Assembly);

// Output Port 與 Driven Adapter 的 DI 綁定
builder.Services.AddScoped<ILoadAccountPort, AccountEfCoreAdapter>();
builder.Services.AddScoped<ILoadTransactionPort, TransactionEfCoreAdapter>();
builder.Services.AddScoped<ILoadPrivilegePort, PrivilegeCacheAdapter>(); // Decorator
```

---

## 9. API 設計規範

### 9.1 RESTful Endpoints

| Method | Path | 說明 |
|--------|------|------|
| `GET` | `/api/v1/accounts/{accountId}/transactions/twd` | 台幣活存交易紀錄 |
| `GET` | `/api/v1/accounts/{accountId}/transactions/fx` | 外幣活存交易紀錄 |
| `GET` | `/api/v1/customers/me/privileges/transfer` | 轉帳優惠內容查詢 |
| `GET` | `/api/v1/customers/me/privileges/transfer/{privilegeId}/usage` | 優惠使用紀錄 |

### 9.2 Driving Adapter — ASP.NET Core Controller

```csharp
[ApiController]
[Route("api/v1/accounts")]
[Authorize]  // JWT 認證
public sealed class AccountController : ControllerBase
{
    private readonly ISender _sender;

    public AccountController(ISender sender) => _sender = sender;

    [HttpGet("{accountId}/transactions/twd")]
    [ProducesResponseType(typeof(ApiResponse<TwdTransactionHistoryResult>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> GetTwdTransactions(
        [FromRoute] string accountId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        // Controller 只負責 HTTP 轉換，不含任何業務邏輯
        var customerId = CustomerId.Of(User.GetCustomerId());  // JWT Claim 萃取

        var query = new GetTwdTransactionHistoryQuery(
            customerId,
            new AccountId(accountId),
            new DateRange(startDate, endDate),
            page,
            size);

        // 透過 ISender 發送至 MediatR，不直接依賴 Handler
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<TwdTransactionHistoryResult>.Success(result));
    }

    [HttpGet("{accountId}/transactions/fx")]
    public async Task<IActionResult> GetFxTransactions(
        [FromRoute] string accountId,
        [FromQuery] string currency,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFxTransactionHistoryQuery(
            CustomerId.Of(User.GetCustomerId()),
            new AccountId(accountId),
            Enum.Parse<Currency>(currency, ignoreCase: true),
            new DateRange(startDate, endDate),
            page, size);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<FxTransactionHistoryResult>.Success(result));
    }
}
```

### 9.3 全域例外處理（Infrastructure/Adapters/In/Web/）

```csharp
// .NET 9 推薦使用 IExceptionHandler
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errorCode) = exception switch
        {
            AccountNotFoundException        => (404, "ACCOUNT_NOT_FOUND"),
            AccountNotOwnedByCustomerException => (403, "ACCOUNT_NOT_OWNED_BY_CUSTOMER"),
            AccountNotActiveException       => (422, "ACCOUNT_NOT_ACTIVE"),
            QueryRangeExceededException     => (422, "QUERY_RANGE_EXCEEDED"),
            QueryValidationException        => (400, "VALIDATION_FAILED"),
            PrivilegeNotFoundException      => (404, "PRIVILEGE_NOT_FOUND"),
            PrivilegeNotOwnedByCustomerException => (403, "PRIVILEGE_NOT_OWNED_BY_CUSTOMER"),
            _                              => (500, "INTERNAL_SERVER_ERROR")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
            Code: errorCode,
            Message: exception.Message,
            Timestamp: DateTimeOffset.UtcNow), cancellationToken);

        return true;
    }
}

// 統一回應格式
public sealed record ApiResponse<T>(string Code, T Data, DateTimeOffset Timestamp)
{
    public static ApiResponse<T> Success(T data) =>
        new("SUCCESS", data, DateTimeOffset.UtcNow);
}

public sealed record ApiErrorResponse(string Code, string Message, DateTimeOffset Timestamp);
```

### 9.4 HTTP 狀態碼規範

| 狀態碼 | 情境 |
|--------|------|
| `200` | 查詢成功 |
| `400` | Query 參數驗證失敗（FluentValidation in Pipeline）|
| `401` | JWT 未提供或無效 |
| `403` | 帳戶/優惠不屬於目前認證客戶 |
| `404` | 帳戶或優惠不存在 |
| `422` | 業務規則違反（查詢區間超過 13 個月、帳戶凍結）|
| `500` | 系統錯誤 |

---

## 10. TDD 設計規劃

### 10.1 測試策略（由內而外）

```
Domain Layer Tests     →  Application Layer Tests  →  Adapter Tests  →  E2E / BDD
(xUnit, 純 C#)            (xUnit + NSubstitute)       (WebApplicationFactory) (SpecFlow)
無任何 Mock               Mock Output Ports             整合測試              Testcontainers
最快速、最純粹            驗證 Handler 協調流程
```

### 10.2 Domain Layer 單元測試

```csharp
// MoneyTests.cs
public sealed class MoneyTests
{
    [Fact(DisplayName = "相同幣別相加應回傳正確金額")]
    public void Add_SameCurrency_ReturnsCorrectAmount()
    {
        var m1 = Money.Twd(1000m);
        var m2 = Money.Twd(500m);

        var result = m1.Add(m2);

        result.Amount.Should().Be(1500m);
        result.Currency.Should().Be(Currency.TWD);
    }

    [Fact(DisplayName = "不同幣別相加應拋出 CurrencyMismatchException")]
    public void Add_DifferentCurrencies_ThrowsCurrencyMismatchException()
    {
        var twd = Money.Twd(1000m);
        var usd = new Money(30m, Currency.USD);

        var act = () => twd.Add(usd);

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact(DisplayName = "負數金額應拋出 ArgumentException")]
    public void Constructor_NegativeAmount_ThrowsArgumentException()
    {
        var act = () => new Money(-1m, Currency.TWD);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*金額不可為負數*");
    }
}

// AccountTests.cs
public sealed class AccountTests
{
    [Fact(DisplayName = "帳戶持有人驗證所有權應通過")]
    public void VerifyOwnership_ByOwner_DoesNotThrow()
    {
        var ownerId = CustomerId.Of("C001");
        var account = AccountTestBuilder.ActiveTwdAccount(ownerId);

        var act = () => account.VerifyOwnership(ownerId);

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "非持有人驗證所有權應拋出 AccountNotOwnedByCustomerException")]
    public void VerifyOwnership_ByNonOwner_ThrowsException()
    {
        var account = AccountTestBuilder.ActiveTwdAccount(CustomerId.Of("C001"));

        var act = () => account.VerifyOwnership(CustomerId.Of("C999"));

        act.Should().Throw<AccountNotOwnedByCustomerException>();
    }

    [Fact(DisplayName = "FilterByDateRange 超過 13 個月應拋出 QueryRangeExceededException")]
    public void FilterByDateRange_ExceedsThirteenMonths_ThrowsException()
    {
        var account = AccountTestBuilder.ActiveTwdAccount();
        var invalidRange = new DateRange(
            DateOnly.FromDateTime(DateTime.Today.AddMonths(-14)),
            DateOnly.FromDateTime(DateTime.Today));

        var act = () => account.FilterByDateRange([], invalidRange);

        act.Should().Throw<QueryRangeExceededException>()
            .WithMessage("*13 個月*");
    }

    [Fact(DisplayName = "FilterByDateRange 應只回傳區間內的交易")]
    public void FilterByDateRange_ValidRange_ReturnsOnlyMatchingTransactions()
    {
        var account = AccountTestBuilder.ActiveTwdAccount();
        var range = new DateRange(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 31));

        var transactions = new List<Transaction>
        {
            TransactionTestBuilder.On(new DateTime(2025, 1, 10)),
            TransactionTestBuilder.On(new DateTime(2025, 2, 5))  // 區間外
        };

        var history = account.FilterByDateRange(transactions, range);

        history.Count.Should().Be(1);
        history.Transactions[0].TransactionDate.Month.Should().Be(1);
    }
}
```

### 10.3 Application Layer 測試（NSubstitute — Mock Output Ports）

```csharp
// GetTwdTransactionHistoryHandlerTests.cs
public sealed class GetTwdTransactionHistoryHandlerTests
{
    private readonly ILoadAccountPort _loadAccountPort = Substitute.For<ILoadAccountPort>();
    private readonly ILoadTransactionPort _loadTransactionPort = Substitute.For<ILoadTransactionPort>();
    private readonly GetTwdTransactionHistoryHandler _handler;

    public GetTwdTransactionHistoryHandlerTests()
    {
        _handler = new GetTwdTransactionHistoryHandler(
            _loadAccountPort, _loadTransactionPort);
    }

    [Fact(DisplayName = "成功查詢台幣交易紀錄")]
    public async Task Handle_ValidQuery_ReturnsTransactionHistory()
    {
        // Arrange
        var query = QueryFixture.TwdQuery("C001", "00123456789012");
        var mockAccount = AccountTestBuilder.ActiveTwdAccount(CustomerId.Of("C001"));
        var mockTransactions = TransactionTestBuilder.SampleList();

        _loadAccountPort.FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(mockAccount);
        _loadTransactionPort.FindByAccountIdAsync(
            Arg.Any<AccountId>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(mockTransactions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Transactions.Should().NotBeEmpty();
        await _loadAccountPort.Received(1)
            .FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "帳戶不存在應拋出 AccountNotFoundException")]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        _loadAccountPort.FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var act = () => _handler.Handle(
            QueryFixture.TwdQuery("C001", "00123456789012"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotFoundException>();
        await _loadTransactionPort.DidNotReceive()
            .FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "非帳戶持有人查詢應拋出 AccountNotOwnedByCustomerException")]
    public async Task Handle_NonOwner_ThrowsAccountNotOwnedByCustomerException()
    {
        // Account 持有人 C001，查詢者 C999
        var mockAccount = AccountTestBuilder.ActiveTwdAccount(CustomerId.Of("C001"));
        _loadAccountPort.FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(mockAccount);

        var act = () => _handler.Handle(
            QueryFixture.TwdQuery("C999", "00123456789012"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotOwnedByCustomerException>();
        await _loadTransactionPort.DidNotReceive()
            .FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>());
    }
}
```

### 10.4 Pipeline Behavior 測試

```csharp
// ValidationBehaviorTests.cs
public sealed class ValidationBehaviorTests
{
    [Fact(DisplayName = "Query 參數不合法應拋出 QueryValidationException")]
    public async Task Handle_InvalidQuery_ThrowsQueryValidationException()
    {
        var validators = new List<IValidator<GetTwdTransactionHistoryQuery>>
        {
            new GetTwdTransactionHistoryQueryValidator()
        };
        var behavior = new ValidationBehavior<GetTwdTransactionHistoryQuery,
                                              TwdTransactionHistoryResult>(validators);

        // size = 0 違反 InclusiveBetween(1, 100)
        var invalidQuery = QueryFixture.TwdQuery("C001", "00123456789012", size: 0);

        var act = () => behavior.Handle(
            invalidQuery,
            () => Task.FromResult(new TwdTransactionHistoryResult("", [], PageInfo.Empty)),
            CancellationToken.None);

        await act.Should().ThrowAsync<QueryValidationException>();
    }
}
```

### 10.5 Driving Adapter 測試（WebApplicationFactory）

```csharp
// AccountControllerTests.cs
public sealed class AccountControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AccountControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 替換 Output Port 為 In-Memory Stub
                services.AddScoped<ILoadAccountPort, InMemoryAccountPort>();
                services.AddScoped<ILoadTransactionPort, InMemoryTransactionPort>();
            });
        }).CreateClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", JwtTestTokenFactory.ForCustomer("C001"));
    }

    [Fact(DisplayName = "成功查詢台幣交易紀錄應回傳 200")]
    public async Task GetTwdTransactions_ValidRequest_Returns200()
    {
        var response = await _client.GetAsync(
            "/api/v1/accounts/00123456789012/transactions/twd" +
            "?startDate=2025-01-01&endDate=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TwdTransactionHistoryResult>>();
        body!.Code.Should().Be("SUCCESS");
    }

    [Fact(DisplayName = "缺少 startDate 應回傳 400")]
    public async Task GetTwdTransactions_MissingStartDate_Returns400()
    {
        var response = await _client.GetAsync(
            "/api/v1/accounts/00123456789012/transactions/twd?endDate=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

---

## 11. BDD 設計規劃

### 11.1 工具選型

| 工具 | 用途 |
|------|------|
| **SpecFlow 4** | BDD Framework（.NET 原生 Gherkin 支援）|
| **Gherkin** | Feature 情境語言（繁體中文）|
| **Testcontainers for .NET** | PostgreSQL / Redis 真實容器 |
| **WireMock.Net** | Mock Core Banking 外部 HTTP API |
| **WebApplicationFactory** | ASP.NET Core Integration Test Host |

### 11.2 Feature 文件

#### 11.2.1 台幣交易紀錄查詢

```gherkin
# Features/Account/TwdTransactionHistory.feature
# language: zh-TW
Feature: 台幣活存帳戶交易紀錄查詢
  作為一位已認證的銀行客戶
  我想要查詢我的台幣活存帳戶交易紀錄
  以便了解帳戶資金進出狀況

  Background:
    Given 客戶 "C001" 已完成身份認證
    And 客戶 "C001" 名下有台幣帳戶 "00123456789012"
    And 帳戶 "00123456789012" 在 2025 年 1 月有以下交易:
      | 日期       | 類型 | 金額  | 說明     |
      | 2025-01-05 | 存入 | 50000 | 薪資轉帳 |
      | 2025-01-10 | 提出 | 10000 | ATM 提款 |
      | 2025-01-20 | 存入 | 3000  | 利息入帳 |

  Scenario: 成功查詢指定日期區間的交易紀錄
    When 客戶查詢帳戶 "00123456789012" 從 "2025-01-01" 到 "2025-01-31" 的台幣交易紀錄
    Then 回應狀態碼為 200
    And 應回傳 3 筆交易紀錄
    And 第一筆交易類型為 "存入" 且金額為 "50000.00"

  Scenario: 查詢超過 13 個月區間應失敗
    When 客戶查詢帳戶 "00123456789012" 從 "2023-12-01" 到 "2025-02-01" 的台幣交易紀錄
    Then 回應狀態碼為 422
    And 錯誤代碼為 "QUERY_RANGE_EXCEEDED"

  Scenario: 查詢不屬於自己的帳戶應被拒絕
    Given 帳戶 "00999999999999" 屬於其他客戶
    When 客戶 "C001" 嘗試查詢帳戶 "00999999999999" 的台幣交易紀錄
    Then 回應狀態碼為 403
    And 錯誤代碼為 "ACCOUNT_NOT_OWNED_BY_CUSTOMER"

  Scenario: 帳戶狀態為凍結時查詢應失敗
    Given 帳戶 "00123456789012" 狀態為 "凍結"
    When 客戶查詢帳戶 "00123456789012" 從 "2025-01-01" 到 "2025-01-31" 的台幣交易紀錄
    Then 回應狀態碼為 422
    And 錯誤代碼為 "ACCOUNT_NOT_ACTIVE"
```

#### 11.2.2 轉帳優惠查詢

```gherkin
# Features/Privilege/TransferPrivilege.feature
Feature: 轉帳優惠查詢
  作為一位已認證的銀行客戶
  我想要查詢目前可用的轉帳優惠內容與使用紀錄

  Background:
    Given 客戶 "C001" 已完成身份認證

  Scenario: 成功查詢有效的轉帳優惠
    Given 客戶 "C001" 有以下轉帳優惠:
      | 優惠ID | 優惠類型       | 總次數 | 已用次數 | 有效期起    | 有效期訖    |
      | P001   | 免手續費跨行轉帳 | 10    | 3       | 2025-01-01 | 2025-12-31 |
    When 客戶查詢轉帳優惠內容
    Then 回應狀態碼為 200
    And 優惠 "P001" 剩餘次數為 7
    And 優惠 "P001" 狀態為有效

  Scenario: 查詢不屬於自己的優惠使用紀錄應被拒絕
    Given 優惠 "P999" 屬於其他客戶
    When 客戶 "C001" 嘗試查詢優惠 "P999" 的使用紀錄
    Then 回應狀態碼為 403
    And 錯誤代碼為 "PRIVILEGE_NOT_OWNED_BY_CUSTOMER"
```

### 11.3 Step Definitions 架構

```csharp
// Steps/AccountSteps.cs
[Binding]
public sealed class AccountSteps
{
    private readonly HttpClient _client;
    private readonly AccountTestDataSetup _dataSetup;
    private HttpResponseMessage _response = default!;

    public AccountSteps(CustomWebApplicationFactory factory, AccountTestDataSetup dataSetup)
    {
        _client = factory.CreateClient();
        _dataSetup = dataSetup;
    }

    [Given(@"客戶 ""(.*)"" 已完成身份認證")]
    public void GivenCustomerIsAuthenticated(string customerId)
    {
        _client.DefaultRequestHeaders.Authorization =
            new("Bearer", JwtTestTokenFactory.ForCustomer(customerId));
    }

    [Given(@"帳戶 ""(.*)"" 在 (.*) 年 (.*) 月有以下交易:")]
    public async Task GivenAccountHasTransactions(
        string accountId, int year, int month, Table table)
    {
        await _dataSetup.InsertTransactionsAsync(accountId, year, month, table.Rows);
    }

    [When(@"客戶查詢帳戶 ""(.*)"" 從 ""(.*)"" 到 ""(.*)"" 的台幣交易紀錄")]
    public async Task WhenQueryTwdTransactions(
        string accountId, string startDate, string endDate)
    {
        _response = await _client.GetAsync(
            $"/api/v1/accounts/{accountId}/transactions/twd" +
            $"?startDate={startDate}&endDate={endDate}");
    }

    [Then(@"回應狀態碼為 (.*)")]
    public void ThenStatusCodeIs(int expectedCode)
    {
        ((int)_response.StatusCode).Should().Be(expectedCode);
    }

    [Then(@"應回傳 (.*) 筆交易紀錄")]
    public async Task ThenTransactionCountIs(int expectedCount)
    {
        var body = await _response.Content
            .ReadFromJsonAsync<ApiResponse<TwdTransactionHistoryResult>>();
        body!.Data.Transactions.Should().HaveCount(expectedCount);
    }

    [Then(@"錯誤代碼為 ""(.*)""")]
    public async Task ThenErrorCodeIs(string expectedCode)
    {
        var body = await _response.Content
            .ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be(expectedCode);
    }
}
```

---

## 12. 專案結構

```
BankAccountQuery/
├── src/
│   ├── BankAccountQuery.Domain/                 ← 純粹核心，零 NuGet 依賴
│   │   ├── Model/
│   │   │   ├── Account/
│   │   │   │   ├── Account.cs                   # Aggregate Root
│   │   │   │   ├── AccountId.cs                 # Value Object
│   │   │   │   ├── AccountType.cs               # Enum (TWD/FX)
│   │   │   │   ├── AccountStatus.cs             # Enum (Active/Frozen/Closed)
│   │   │   │   ├── Transaction.cs               # Entity
│   │   │   │   ├── TransactionId.cs             # Value Object
│   │   │   │   ├── TransactionType.cs           # Enum (Credit/Debit)
│   │   │   │   ├── TransactionChannel.cs        # Enum
│   │   │   │   └── TransactionHistory.cs        # Value Object（查詢結果）
│   │   │   ├── Privilege/
│   │   │   │   ├── TransferPrivilege.cs         # Aggregate Root
│   │   │   │   ├── PrivilegeId.cs               # Value Object
│   │   │   │   ├── PrivilegeType.cs             # Enum
│   │   │   │   ├── PrivilegeUsageRecord.cs      # Entity
│   │   │   │   └── PrivilegeUsageHistory.cs     # Value Object
│   │   │   └── Shared/
│   │   │       ├── Money.cs                     # Value Object
│   │   │       ├── Currency.cs                  # Enum / Value Object
│   │   │       ├── DateRange.cs                 # Value Object
│   │   │       └── CustomerId.cs                # Value Object
│   │   └── Exceptions/
│   │       ├── DomainException.cs               # 抽象基底
│   │       ├── AccountNotFoundException.cs
│   │       ├── AccountNotOwnedByCustomerException.cs
│   │       ├── AccountNotActiveException.cs
│   │       ├── QueryRangeExceededException.cs
│   │       ├── CurrencyMismatchException.cs
│   │       ├── PrivilegeNotFoundException.cs
│   │       └── PrivilegeNotOwnedByCustomerException.cs
│   │
│   ├── BankAccountQuery.Application/            ← Use Cases + Port 定義 + MediatR
│   │   ├── Ports/
│   │   │   └── Out/                             # Output Port Interfaces（Repository 在此！）
│   │   │       ├── ILoadAccountPort.cs
│   │   │       ├── ILoadTransactionPort.cs
│   │   │       ├── ILoadPrivilegePort.cs
│   │   │       └── IAuditLogPort.cs
│   │   ├── Queries/
│   │   │   ├── Account/
│   │   │   │   ├── GetTwdTransactionHistoryQuery.cs    # IRequest<TResult>
│   │   │   │   ├── GetTwdTransactionHistoryHandler.cs  # IRequestHandler
│   │   │   │   ├── GetTwdTransactionHistoryQueryValidator.cs
│   │   │   │   ├── GetFxTransactionHistoryQuery.cs
│   │   │   │   ├── GetFxTransactionHistoryHandler.cs
│   │   │   │   ├── GetFxTransactionHistoryQueryValidator.cs
│   │   │   │   └── Results/
│   │   │   │       ├── TwdTransactionHistoryResult.cs  # Read Model
│   │   │   │       ├── TwdTransactionDto.cs
│   │   │   │       ├── FxTransactionHistoryResult.cs   # Read Model
│   │   │   │       ├── FxTransactionDto.cs
│   │   │   │       └── PageInfo.cs
│   │   │   └── Privilege/
│   │   │       ├── GetTransferPrivilegeQuery.cs
│   │   │       ├── GetTransferPrivilegeHandler.cs
│   │   │       ├── GetPrivilegeUsageHistoryQuery.cs
│   │   │       ├── GetPrivilegeUsageHistoryHandler.cs
│   │   │       └── Results/
│   │   │           ├── TransferPrivilegeResult.cs      # Read Model
│   │   │           ├── TransferPrivilegeDto.cs
│   │   │           ├── PrivilegeUsageHistoryResult.cs  # Read Model
│   │   │           └── PrivilegeUsageDto.cs
│   │   ├── Behaviors/                           # MediatR Pipeline Behaviors
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── AuditLogBehavior.cs
│   │   └── Common/
│   │       ├── ICustomerQuery.cs                # Marker Interface（AuditLog 識別用）
│   │       └── QueryValidationException.cs
│   │
│   ├── BankAccountQuery.Infrastructure/         ← Adapters（實作 Ports）
│   │   ├── Adapters/
│   │   │   ├── In/Web/                          # Driving Adapters
│   │   │   │   ├── AccountController.cs         # 呼叫 ISender
│   │   │   │   ├── PrivilegeController.cs
│   │   │   │   ├── GlobalExceptionHandler.cs    # IExceptionHandler
│   │   │   │   └── Extensions/
│   │   │   │       └── ClaimsPrincipalExtensions.cs  # JWT Claim 萃取
│   │   │   └── Out/                             # Driven Adapters
│   │   │       ├── Persistence/                 # 實作 Output Port（EF Core）
│   │   │       │   ├── AccountEfCoreAdapter.cs  # 實作 ILoadAccountPort
│   │   │       │   ├── TransactionEfCoreAdapter.cs
│   │   │       │   ├── PrivilegeEfCoreAdapter.cs
│   │   │       │   ├── BankDbContext.cs
│   │   │       │   └── Entities/                # EF Core Entities（僅限 Infrastructure）
│   │   │       ├── CoreBanking/                 # 實作 Output Port（HTTP）
│   │   │       │   └── CoreBankingHttpAdapter.cs
│   │   │       ├── Cache/                       # 實作 Output Port（Redis）
│   │   │       │   └── PrivilegeCacheAdapter.cs # Decorator Pattern
│   │   │       └── AuditLog/
│   │   │           └── PostgresAuditLogAdapter.cs
│   │   └── Configuration/
│   │       ├── DependencyInjection.cs           # DI 註冊（MediatR、Validators、Adapters）
│   │       └── Migrations/                      # EF Core Migrations
│   │
│   └── BankAccountQuery.Api/                    ← Entry Point
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── BankAccountQuery.Domain.Tests/           # Domain Unit Tests（純 C#）
│   │   ├── Model/Account/
│   │   │   ├── AccountTests.cs
│   │   │   └── TransactionHistoryTests.cs
│   │   ├── Model/Privilege/
│   │   │   └── TransferPrivilegeTests.cs
│   │   └── Model/Shared/
│   │       ├── MoneyTests.cs
│   │       └── DateRangeTests.cs
│   ├── BankAccountQuery.Application.Tests/      # Application Unit Tests（NSubstitute）
│   │   ├── Queries/
│   │   │   ├── GetTwdTransactionHistoryHandlerTests.cs
│   │   │   ├── GetFxTransactionHistoryHandlerTests.cs
│   │   │   ├── GetTransferPrivilegeHandlerTests.cs
│   │   │   └── GetPrivilegeUsageHistoryHandlerTests.cs
│   │   ├── Behaviors/
│   │   │   ├── LoggingBehaviorTests.cs
│   │   │   └── ValidationBehaviorTests.cs
│   │   └── Fixtures/                            # 共用 Test Builders
│   │       ├── AccountTestBuilder.cs
│   │       ├── TransactionTestBuilder.cs
│   │       └── QueryFixture.cs
│   ├── BankAccountQuery.Infrastructure.Tests/   # Adapter Integration Tests
│   │   ├── Persistence/
│   │   │   └── AccountEfCoreAdapterTests.cs     # Testcontainers PostgreSQL
│   │   └── Web/
│   │       ├── AccountControllerTests.cs        # WebApplicationFactory
│   │       └── PrivilegeControllerTests.cs
│   └── BankAccountQuery.BddTests/               # BDD Integration Tests（SpecFlow）
│       ├── Features/
│       │   ├── Account/
│       │   │   ├── TwdTransactionHistory.feature
│       │   │   └── FxTransactionHistory.feature
│       │   └── Privilege/
│       │       ├── TransferPrivilege.feature
│       │       └── PrivilegeUsageHistory.feature
│       ├── Steps/
│       │   ├── AccountSteps.cs
│       │   └── PrivilegeSteps.cs
│       └── Support/
│           ├── CustomWebApplicationFactory.cs
│           ├── AccountTestDataSetup.cs
│           └── JwtTestTokenFactory.cs
│
├── BankAccountQuery.sln
└── README.md
```

---

## 13. 工項清單 Work Breakdown Structure

### Sprint 0 — 環境建置（1 週）

| 工項 ID | 工項名稱 | 說明 | 人天 |
|---------|----------|------|------|
| S0-01 | 建立 Solution 骨架 | 4 個 Projects + 4 個 Test Projects | 0.5 |
| S0-02 | 設定 .csproj 依賴 | MediatR、FluentValidation、EF Core、StackExchange.Redis | 0.5 |
| S0-03 | Testcontainers 設定 | PostgreSQL + Redis 容器 | 1.0 |
| S0-04 | WireMock.Net 設定 | Mock Core Banking API | 0.5 |
| S0-05 | CI Pipeline | GitHub Actions，含測試報告 + SpecFlow HTML Report | 1.0 |
| S0-06 | Swagger / OpenAPI 設定 | Swashbuckle + JWT Bearer 設定 | 0.5 |

### Sprint 1 — Domain Layer（2 週）

| 工項 ID | 工項名稱 | 說明 | 人天 |
|---------|----------|------|------|
| S1-01 | Shared Value Objects | Money、DateRange、CustomerId、Currency | 1.0 |
| S1-02 | AccountId Value Object | 含 14 位數字格式驗證 | 0.5 |
| S1-03 | Transaction Entity | 含 Money、TransactionChannel | 0.5 |
| S1-04 | TransactionHistory Value Object | IReadOnlyList 不可變封裝 | 0.5 |
| S1-05 | Account Aggregate Root | VerifyOwnership、EnsureActive、FilterByDateRange | 1.5 |
| S1-06 | PrivilegeUsageRecord Entity | 含使用日期、節省金額 | 0.5 |
| S1-07 | TransferPrivilege Aggregate Root | IsValid、GetRemainingQuota、FilterUsageHistory | 1.5 |
| S1-08 | Domain Exceptions | DomainException 抽象 + 所有業務例外 | 0.5 |
| S1-09 | Domain Layer 單元測試 | xUnit + FluentAssertions，完整 TDD | 2.5 |

### Sprint 2 — Application Layer（1.5 週）

| 工項 ID | 工項名稱 | 說明 | 人天 |
|---------|----------|------|------|
| S2-01 | Output Port Interfaces | ILoadAccountPort、ILoadTransactionPort、ILoadPrivilegePort、IAuditLogPort | 0.5 |
| S2-02 | Query Records | 4 個 IRequest<TResult> record + ICustomerQuery | 0.5 |
| S2-03 | GetTwdTransactionHistoryHandler | 含 Read Model 轉換 | 1.0 |
| S2-04 | GetFxTransactionHistoryHandler | 含幣別呈現 | 1.0 |
| S2-05 | GetTransferPrivilegeHandler | 含 IsValid 狀態映射 | 0.5 |
| S2-06 | GetPrivilegeUsageHistoryHandler | 含使用紀錄分頁 | 0.5 |
| S2-07 | Read Models（DTOs）| 8 個 Result / DTO record | 1.0 |
| S2-08 | FluentValidation Validators | 4 個 Query Validators | 0.5 |
| S2-09 | LoggingBehavior | IPipelineBehavior 實作 | 0.5 |
| S2-10 | ValidationBehavior | IPipelineBehavior + FluentValidation 整合 | 0.5 |
| S2-11 | AuditLogBehavior | IPipelineBehavior + IAuditLogPort 整合 | 0.5 |
| S2-12 | Application Layer 單元測試 | NSubstitute Mock Output Ports，4 個 Handler + 2 個 Behavior | 2.5 |

### Sprint 3 — Infrastructure Adapters（1.5 週）

| 工項 ID | 工項名稱 | 說明 | 人天 |
|---------|----------|------|------|
| S3-01 | AccountController（Driving Adapter）| 2 個端點、ISender.Send | 1.5 |
| S3-02 | PrivilegeController（Driving Adapter）| 2 個端點 | 1.0 |
| S3-03 | GlobalExceptionHandler | IExceptionHandler，Domain Exception → HTTP Status | 1.0 |
| S3-04 | EF Core Entity Schema | Account、Transaction、Privilege 資料表 + Migration | 1.0 |
| S3-05 | AccountEfCoreAdapter | 實作 ILoadAccountPort | 1.0 |
| S3-06 | TransactionEfCoreAdapter | 實作 ILoadTransactionPort，含分頁 | 1.0 |
| S3-07 | PrivilegeEfCoreAdapter | 實作 ILoadPrivilegePort（DB 版）| 0.5 |
| S3-08 | CoreBankingHttpAdapter | HttpClient + WireMock.Net 驗證 | 1.5 |
| S3-09 | PrivilegeCacheAdapter | Redis Decorator，包裝 PrivilegeEfCoreAdapter | 1.0 |
| S3-10 | DI 註冊（DependencyInjection.cs）| MediatR、Validators、Behaviors、Adapters | 0.5 |
| S3-11 | Controller 整合測試（WebApplicationFactory）| 4 個端點完整測試 | 1.5 |
| S3-12 | EF Core Adapter 整合測試 | Testcontainers PostgreSQL | 1.0 |

### Sprint 4 — BDD Integration Tests（1 週）

| 工項 ID | 工項名稱 | 說明 | 人天 |
|---------|----------|------|------|
| S4-01 | 台幣交易紀錄 Feature 文件 | 含 Error Path Scenario | 0.5 |
| S4-02 | 外幣交易紀錄 Feature 文件 | 含幣別驗證情境 | 0.5 |
| S4-03 | 轉帳優惠查詢 Feature 文件 | 含過期/額度用盡情境 | 0.5 |
| S4-04 | 優惠使用紀錄 Feature 文件 | 含越權情境 | 0.5 |
| S4-05 | AccountSteps 實作 | SpecFlow Step Definitions | 1.5 |
| S4-06 | PrivilegeSteps 實作 | SpecFlow Step Definitions | 1.0 |
| S4-07 | Test Builders + TestDataSetup | AccountTestBuilder、Testcontainers 資料建立 | 1.0 |
| S4-08 | SpecFlow HTML Report 整合 | CI 輸出 LivingDoc 報告 | 0.5 |

### Sprint 5 — 安全性與觀測性（0.5 週）

| 工項 ID | 工項名稱 | 說明 | 人天 |
|---------|----------|------|------|
| S5-01 | JWT 認證整合 | ASP.NET Core JWT Bearer + CustomerId Claim | 1.0 |
| S5-02 | Prometheus Metrics | OpenTelemetry + Prometheus，Query 延遲/錯誤率 | 0.5 |
| S5-03 | Health Check | ASP.NET Core Health Checks（DB / Redis / CoreBanking）| 0.5 |
| S5-04 | OpenAPI 文件補全 | Swashbuckle 端點描述、錯誤碼說明 | 0.5 |

### 工項彙總

| Sprint | 主題 | 人天 |
|--------|------|------|
| Sprint 0 | 環境建置 | 4.0 |
| Sprint 1 | Domain Layer | 9.0 |
| Sprint 2 | Application Layer | 9.5 |
| Sprint 3 | Infrastructure Adapters | 11.5 |
| Sprint 4 | BDD Integration Tests | 6.0 |
| Sprint 5 | 安全性與觀測性 | 2.5 |
| **合計** | | **42.5 人天** |

---

## 14. 技術選型說明

### 14.1 核心 NuGet 套件

```xml
<!-- BankAccountQuery.Domain.csproj — 零依賴 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <!-- 無任何 NuGet 依賴 -->
</Project>

<!-- BankAccountQuery.Application.csproj -->
<ItemGroup>
  <PackageReference Include="MediatR" Version="12.x" />
  <PackageReference Include="FluentValidation" Version="11.x" />
  <ProjectReference Include="..\BankAccountQuery.Domain\BankAccountQuery.Domain.csproj" />
</ItemGroup>

<!-- BankAccountQuery.Infrastructure.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.PostgreSQL" Version="9.x" />
  <PackageReference Include="StackExchange.Redis" Version="2.x" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.x" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="7.x" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.x" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.x" />
  <ProjectReference Include="..\BankAccountQuery.Application\..." />
</ItemGroup>

<!-- Test Projects -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.x" />
  <PackageReference Include="FluentAssertions" Version="6.x" />
  <PackageReference Include="NSubstitute" Version="5.x" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.x" />
  <PackageReference Include="WireMock.Net" Version="1.x" />
  <PackageReference Include="SpecFlow.xUnit" Version="3.x" />
  <PackageReference Include="SpecFlow.Plus.LivingDocPlugin" Version="3.x" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.x" />
</ItemGroup>
```

### 14.2 .NET 9 特性應用

| .NET 特性 | 應用場景 |
|-----------|----------|
| **record / record struct** | Value Objects（DateRange）、Query Records、Read Model DTOs |
| **Primary Constructor** | Handler 依賴注入、Behavior 建構子 |
| **Pattern Matching（switch expression）**| GlobalExceptionHandler Exception → HTTP Status 映射 |
| **IExceptionHandler** | .NET 8+ 統一例外處理機制，取代 Middleware |
| **Nullable Reference Types** | 全專案啟用，`Account?` 明確表達可能為 null |
| **Top-level statements** | Program.cs 精簡 |

---

## 15. 非功能性需求考量

### 15.1 效能目標

| 指標 | 目標值 |
|------|--------|
| API 回應時間（P99）| < 500ms |
| API 回應時間（P95）| < 200ms |
| QPS 峰值 | ≥ 500 |
| 30 天交易紀錄查詢 | < 300ms |

### 15.2 安全性要求

- JWT Bearer Token 驗證（RS256 非對稱加密）
- 帳戶/優惠所有權驗證由 **Domain Aggregate 強制執行**
- Audit Log 由 **AuditLogBehavior 統一攔截**，Handler 無需自行處理
- HTTPS Only，TLS 1.2+

### 15.3 測試覆蓋率目標

| 層級 | 目標 |
|------|------|
| Domain Layer | ≥ 90% |
| Application Layer（Handler + Behavior）| ≥ 85% |
| Infrastructure Adapters | ≥ 80% |
| BDD Scenarios | 涵蓋所有 Happy Path + 主要 Error Path |

### 15.4 快取策略

```
轉帳優惠內容（更新頻率低）
  → PrivilegeCacheAdapter（Decorator）包裝 PrivilegeEfCoreAdapter
  → Redis Cache，TTL = 5 分鐘
  → Cache Key: privilege:customer:{customerId}

台幣 / 外幣交易紀錄（即時性要求高）
  → 不快取，直接查詢 EF Core Read Model
  → 使用 EF Core AsNoTracking() 提升查詢效能
```

---

## 16. 附錄：SOLID 原則對應表

| 原則 | 在本架構的體現 |
|------|---------------|
| **S 單一職責** | Account Aggregate 只負責帳戶業務規則；Handler 只負責協調流程；Controller 只負責 HTTP 轉換；Pipeline Behavior 各自負責一個橫切關注點 |
| **O 開放封閉** | 新增查詢功能只需新增 Query Record + Handler + Validator + Feature，不修改既有 Pipeline；新增 Behavior 只需實作 `IPipelineBehavior<,>` 並在 DI 註冊 |
| **L 里氏替換** | `PrivilegeCacheAdapter` 與 `PrivilegeEfCoreAdapter` 皆實作 `ILoadPrivilegePort`，可透過 DI 互換而不影響 Handler |
| **I 介面隔離** | `ILoadAccountPort` / `ILoadTransactionPort` 各自獨立；Handler 只依賴自己需要的 Port；`ICustomerQuery` 讓 AuditLogBehavior 只關注含 CustomerId 的 Query |
| **D 依賴倒置** | Handler 依賴 `ILoadAccountPort`（Interface），不依賴 `AccountEfCoreAdapter`（實作）；Controller 依賴 `ISender`，不依賴任何 Handler；Domain Layer 不依賴任何人 |

---

## 17. ADR：Repository Pattern 設計決策

### ADR-001：Repository 操作單位必須為 Aggregate Root

**狀態**：已採用（Accepted）

**決策**：Output Port Interface 的操作單位是 Aggregate Root，不為邊界內的 Entity 建立獨立的 Output Port。

```csharp
// ✅ 正確：以 Aggregate Root 為單位
public interface ISaveAccountPort
{
    Task SaveAsync(Account account, CancellationToken cancellationToken = default);
}

// ❌ 錯誤：繞過 Aggregate Root，直接操作內部 Entity
public interface ISaveTransactionPort
{
    Task SaveAsync(Transaction transaction, CancellationToken cancellationToken = default);
    // Transaction 是 Account 邊界內的 Entity，不應有獨立的 Output Port
}
```

**理由**：若允許直接 `SaveAsync(transaction)`，帳戶凍結時不得新增交易的業務規則將無人守護。
`Account.EnsureActive()` 必須在 Save 前被呼叫，而這只有透過 Aggregate Root 才能保證。

---

### ADR-002：Read Side Output Port 的設計選項評估

**狀態**：已採用方案 A（Accepted — Option A）

**背景**：銀行帳戶交易紀錄可能達數萬筆，若 `FindByAccountIdAsync()` 載入完整 Aggregate（含所有 Transaction），將造成嚴重效能問題。

**評估的三個選項**

#### 方案 A — 分離查詢，Handler 協調，Domain 執行規則（本 Tutorial 採用）

```
Handler 流程：
  Step 1: ILoadAccountPort.FindByAccountIdAsync()
          → 取得 Account（含業務狀態，不含交易明細）
  Step 2: account.VerifyOwnership(customerId)
          → Domain 執行所有權業務規則
  Step 3: account.EnsureActive()
          → Domain 執行帳戶狀態業務規則
  Step 4: ILoadTransactionPort.FindByAccountIdAsync(accountId, dateRange)
          → 取得指定區間的原始交易資料（DB 層初步過濾）
  Step 5: account.FilterByDateRange(rawTransactions, dateRange)
          → Domain 執行查詢區間限制（13 個月）與最終過濾
```

```
優點：
  ✅ 避免 Large Aggregate 載入問題
  ✅ 所有業務規則仍由 Domain Model 執行
  ✅ Handler 流程清晰，每步驟職責明確

缺點：
  ⚠ Transaction 在 Step 4 暫時脫離 Aggregate 邊界
  ⚠ 需靠設計紀律確保 Handler 不對 rawTransactions 執行業務判斷
```

#### 方案 B — Lazy Loading

```csharp
public sealed class Account
{
    private readonly Func<Task<IReadOnlyList<Transaction>>> _transactionsLoader;
    // Domain Model 隱含對外部載入機制的依賴 → 破壞 Domain 純粹性 ❌
}
```

#### 方案 C — CQRS 徹底分離（Query Side 完全不走 Aggregate）

```
Handler → ILoadTwdTransactionReadModelPort.QueryAsync(accountId, dateRange)
        → EF Core 直接回傳 TwdTransactionDto
        → 所有權驗證須移至 Application Layer 的 if 判斷 ❌
```

**決策矩陣**

| 評估項目 | 方案 A（採用）| 方案 B | 方案 C |
|----------|--------------|--------|--------|
| Domain 純粹性 | ✅ | ❌ | ⚠ 放棄 |
| 業務規則集中 | ✅ Domain | ✅ Domain | ❌ Application Layer |
| Large Aggregate 效能 | ✅ | ✅ | ✅ 最佳 |
| 測試難度 | ✅ 低 | ⚠ 中 | ⚠ 中 |

**採用方案 A 的配套設計紀律**

```
規則 1：ILoadTransactionPort 回傳的 IReadOnlyList<Transaction>
        只可傳入 Domain Method（account.FilterByDateRange(...)），
        Handler 不得直接對此集合執行任何業務判斷。

規則 2：Handler 中若出現針對 Transaction 的 Where / Any 業務邏輯，
        視為 Code Smell，應重構回 Domain Method。

規則 3：Transaction 不建立獨立的 ISaveTransactionPort（Write Side）。
```

---

### ADR-003：Output Port 命名規範

**狀態**：已採用（Accepted）

**決策**：採用意圖導向命名（`ILoadXxx` / `ISaveXxx`），不使用 `IXxxRepository`。

| 傳統命名 | 意圖導向命名 | 實作者（Driven Adapter）|
|----------|-------------|------------------------|
| `IAccountRepository.FindById()` | `ILoadAccountPort` | `AccountEfCoreAdapter` |
| `IAccountRepository.Save()` | `ISaveAccountPort` | `AccountEfCoreAdapter` |
| `ITransactionRepository.FindByAccountId()` | `ILoadTransactionPort` | `TransactionEfCoreAdapter` |
| `IPrivilegeRepository.FindByCustomerId()` | `ILoadPrivilegePort` | `PrivilegeEfCoreAdapter` / `PrivilegeCacheAdapter` |

**理由**：
- 名稱表達 Application Layer 的**意圖**，不暗示持久化技術
- 支援同一 Port 有多種實作（EF Core / Redis Decorator）而不混淆
- 符合 ISP：`ILoadAccountPort` 與 `ISaveAccountPort` 分離，Handler 只依賴自己需要的能力
