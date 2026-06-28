using BankAccountQuery.Application.Commands.Privilege;
using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BankAccountQuery.Application.Tests.Commands;

public sealed class UseTransferPrivilegeHandlerTests
{
    private readonly ILoadPrivilegePort _load = Substitute.For<ILoadPrivilegePort>();
    private readonly ISavePrivilegePort _save = Substitute.For<ISavePrivilegePort>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly UseTransferPrivilegeHandler _handler;

    public UseTransferPrivilegeHandlerTests()
        => _handler = new UseTransferPrivilegeHandler(_load, _save, _dispatcher);

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static TransferPrivilege Privilege(string owner = "C002", int total = 5, int used = 0) =>
        new(PrivilegeId.Of("P010"), CustomerId.Of(owner),
            PrivilegeType.FeeWaiverInterBank, total, used,
            new DateRange(Today.AddYears(-1), Today.AddYears(1)));

    private static UseTransferPrivilegeCommand Command(string customer = "C002") =>
        new(CustomerId.Of(customer), PrivilegeId.Of("P010"), 15m, "跨行轉帳免手續費");

    [Fact(DisplayName = "成功使用優惠：持久化、派發事件並回傳新剩餘次數")]
    public async Task Handle_Valid_SavesDispatchesAndReturnsRemaining()
    {
        _load.FindByPrivilegeIdAsync(Arg.Any<PrivilegeId>(), Arg.Any<CancellationToken>())
            .Returns(Privilege(total: 5, used: 0));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.RemainingQuota.Should().Be(4);
        result.UsageId.Should().NotBeNullOrWhiteSpace();
        await _save.Received(1).SaveAsync(Arg.Any<TransferPrivilege>(), Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IReadOnlyList<IDomainEvent>>(e => e.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "優惠不存在應拋出 PrivilegeNotFoundException 且不持久化")]
    public async Task Handle_NotFound_ThrowsAndDoesNotSave()
    {
        _load.FindByPrivilegeIdAsync(Arg.Any<PrivilegeId>(), Arg.Any<CancellationToken>())
            .Returns((TransferPrivilege?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<PrivilegeNotFoundException>();
        await _save.DidNotReceive().SaveAsync(Arg.Any<TransferPrivilege>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "非持有人使用應拋出例外且不持久化、不派發事件")]
    public async Task Handle_NonOwner_ThrowsAndDoesNotSave()
    {
        _load.FindByPrivilegeIdAsync(Arg.Any<PrivilegeId>(), Arg.Any<CancellationToken>())
            .Returns(Privilege(owner: "C002"));

        var act = () => _handler.Handle(Command(customer: "C999"), CancellationToken.None);

        await act.Should().ThrowAsync<PrivilegeNotOwnedByCustomerException>();
        await _save.DidNotReceive().SaveAsync(Arg.Any<TransferPrivilege>(), Arg.Any<CancellationToken>());
        await _dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "次數用盡時應拋出例外且不持久化")]
    public async Task Handle_QuotaExhausted_ThrowsAndDoesNotSave()
    {
        _load.FindByPrivilegeIdAsync(Arg.Any<PrivilegeId>(), Arg.Any<CancellationToken>())
            .Returns(Privilege(total: 1, used: 1));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<PrivilegeQuotaExhaustedException>();
        await _save.DidNotReceive().SaveAsync(Arg.Any<TransferPrivilege>(), Arg.Any<CancellationToken>());
    }
}
