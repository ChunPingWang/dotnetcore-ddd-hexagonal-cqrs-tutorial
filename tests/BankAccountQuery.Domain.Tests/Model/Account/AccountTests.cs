using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Domain.Tests.TestBuilders;
using FluentAssertions;
using Xunit;

namespace BankAccountQuery.Domain.Tests.Model.Account;

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

    [Fact(DisplayName = "凍結帳戶 EnsureActive 應拋出 AccountNotActiveException")]
    public void EnsureActive_FrozenAccount_Throws()
    {
        var account = AccountTestBuilder.FrozenTwdAccount();

        var act = () => account.EnsureActive();

        act.Should().Throw<AccountNotActiveException>();
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
        var range = new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

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
