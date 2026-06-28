using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;

/// <summary>
/// EF Core 持久化模型（僅限 Infrastructure，與 Domain Model 解耦）。
/// </summary>
public sealed class AccountEntity
{
    public string AccountId { get; set; } = default!;
    public string OwnerId { get; set; } = default!;
    public AccountType AccountType { get; set; }
    public Currency Currency { get; set; }
    public AccountStatus Status { get; set; }

    public Domain.Model.Account.Account ToDomain() =>
        new(
            new AccountId(AccountId),
            CustomerId.Of(OwnerId),
            AccountType,
            Currency,
            Status);

    public static AccountEntity FromDomain(Domain.Model.Account.Account a) => new()
    {
        AccountId = a.AccountId.Value,
        OwnerId = a.OwnerId.Value,
        AccountType = a.AccountType,
        Currency = a.Currency,
        Status = a.Status
    };
}
