using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using Xunit;

namespace BankAccountQuery.Domain.Tests.Model.Shared;

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

    [Fact(DisplayName = "超過 2 位小數應拋出 ArgumentException")]
    public void Constructor_MoreThanTwoDecimals_ThrowsArgumentException()
    {
        var act = () => new Money(1.234m, Currency.TWD);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*2 位小數*");
    }

    [Fact(DisplayName = "金額與幣別相同時應相等")]
    public void Equals_SameAmountAndCurrency_AreEqual()
    {
        Money.Twd(100m).Should().Be(Money.Twd(100m));
        (Money.Twd(100m) == Money.Twd(100m)).Should().BeTrue();
    }
}
