using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Account;
using BankAccountQuery.Application.Tests.Fixtures;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BankAccountQuery.Application.Tests.Queries;

public sealed class GetTwdTransactionHistoryHandlerTests
{
    private readonly ILoadAccountPort _loadAccountPort = Substitute.For<ILoadAccountPort>();
    private readonly ILoadTransactionPort _loadTransactionPort = Substitute.For<ILoadTransactionPort>();
    private readonly GetTwdTransactionHistoryHandler _handler;

    public GetTwdTransactionHistoryHandlerTests()
    {
        _handler = new GetTwdTransactionHistoryHandler(_loadAccountPort, _loadTransactionPort);
    }

    [Fact(DisplayName = "成功查詢台幣交易紀錄")]
    public async Task Handle_ValidQuery_ReturnsTransactionHistory()
    {
        var query = QueryFixture.TwdQuery("C001", "00123456789012");
        var mockAccount = AccountTestBuilder.ActiveTwdAccount(CustomerId.Of("C001"));
        var mockTransactions = TransactionTestBuilder.SampleList();

        _loadAccountPort.FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(mockAccount);
        _loadTransactionPort.FindByAccountIdAsync(
            Arg.Any<AccountId>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(mockTransactions);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Transactions.Should().NotBeEmpty();
        result.PageInfo.TotalCount.Should().Be(3);
        await _loadAccountPort.Received(1)
            .FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "帳戶不存在應拋出 AccountNotFoundException")]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        _loadAccountPort.FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var act = () => _handler.Handle(
            QueryFixture.TwdQuery("C001", "00123456789012"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotFoundException>();
        await _loadTransactionPort.DidNotReceive()
            .FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "非帳戶持有人查詢應拋出 AccountNotOwnedByCustomerException")]
    public async Task Handle_NonOwner_ThrowsAccountNotOwnedByCustomerException()
    {
        var mockAccount = AccountTestBuilder.ActiveTwdAccount(CustomerId.Of("C001"));
        _loadAccountPort.FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(mockAccount);

        var act = () => _handler.Handle(
            QueryFixture.TwdQuery("C999", "00123456789012"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotOwnedByCustomerException>();
        await _loadTransactionPort.DidNotReceive()
            .FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "凍結帳戶查詢應拋出 AccountNotActiveException")]
    public async Task Handle_FrozenAccount_ThrowsAccountNotActiveException()
    {
        var frozen = AccountTestBuilder.FrozenTwdAccount(CustomerId.Of("C001"));
        _loadAccountPort.FindByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(frozen);

        var act = () => _handler.Handle(
            QueryFixture.TwdQuery("C001", "00123456789012"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotActiveException>();
    }
}
