using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Tests.TestBuilders;

public static class AccountTestBuilder
{
    public const string DefaultAccountId = "00123456789012";

    public static Account ActiveTwdAccount(CustomerId? owner = null) =>
        new(new AccountId(DefaultAccountId),
            owner ?? CustomerId.Of("C001"),
            AccountType.Twd, Currency.TWD, AccountStatus.Active);

    public static Account FrozenTwdAccount(CustomerId? owner = null) =>
        new(new AccountId(DefaultAccountId),
            owner ?? CustomerId.Of("C001"),
            AccountType.Twd, Currency.TWD, AccountStatus.Frozen);
}
