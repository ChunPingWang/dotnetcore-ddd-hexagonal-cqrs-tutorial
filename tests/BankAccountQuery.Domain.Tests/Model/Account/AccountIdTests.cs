using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Account;
using FluentAssertions;
using Xunit;

namespace BankAccountQuery.Domain.Tests.Model.Account;

public sealed class AccountIdTests
{
    [Theory(DisplayName = "合法 14 位數字帳號應建立成功")]
    [InlineData("00123456789012")]
    [InlineData("99999999999999")]
    public void Constructor_Valid_Succeeds(string value)
    {
        var id = new AccountId(value);
        id.Value.Should().Be(value);
    }

    [Theory(DisplayName = "非 14 位數字帳號應拋出 InvalidAccountIdFormatException")]
    [InlineData("123")]
    [InlineData("0012345678901")]     // 13 位
    [InlineData("001234567890123")]   // 15 位
    [InlineData("0012345678901A")]    // 含字母
    public void Constructor_Invalid_Throws(string value)
    {
        var act = () => new AccountId(value);
        act.Should().Throw<InvalidAccountIdFormatException>();
    }
}
