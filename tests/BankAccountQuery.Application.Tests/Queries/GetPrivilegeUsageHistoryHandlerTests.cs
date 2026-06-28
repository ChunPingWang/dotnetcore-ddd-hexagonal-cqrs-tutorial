using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Privilege;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BankAccountQuery.Application.Tests.Queries;

public sealed class GetPrivilegeUsageHistoryHandlerTests
{
    private readonly ILoadPrivilegePort _loadPrivilegePort = Substitute.For<ILoadPrivilegePort>();
    private readonly GetPrivilegeUsageHistoryHandler _handler;

    public GetPrivilegeUsageHistoryHandlerTests()
    {
        _handler = new GetPrivilegeUsageHistoryHandler(_loadPrivilegePort);
    }

    private static GetPrivilegeUsageHistoryQuery Query(string customer = "C001", string priv = "P001") =>
        new(
            CustomerId.Of(customer),
            PrivilegeId.Of(priv),
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            0, 20);

    private static TransferPrivilege Privilege(string owner = "C001")
    {
        var records = new List<PrivilegeUsageRecord>
        {
            new("U1", new DateOnly(2025, 1, 12), Money.Twd(15m), "跨行轉帳免手續費"),
            new("U2", new DateOnly(2025, 3, 9), Money.Twd(30m), "跨行轉帳免手續費")
        };
        return new TransferPrivilege(
            PrivilegeId.Of("P001"), CustomerId.Of(owner),
            PrivilegeType.FeeWaiverInterBank, 10, 3,
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            records);
    }

    [Fact(DisplayName = "成功查詢優惠使用紀錄")]
    public async Task Handle_Valid_ReturnsUsageHistory()
    {
        _loadPrivilegePort.FindByPrivilegeIdAsync(Arg.Any<PrivilegeId>(), Arg.Any<CancellationToken>())
            .Returns(Privilege("C001"));

        var result = await _handler.Handle(Query(), CancellationToken.None);

        result.PrivilegeId.Should().Be("P001");
        result.Records.Should().HaveCount(2);
    }

    [Fact(DisplayName = "優惠不存在應拋出 PrivilegeNotFoundException")]
    public async Task Handle_NotFound_Throws()
    {
        _loadPrivilegePort.FindByPrivilegeIdAsync(Arg.Any<PrivilegeId>(), Arg.Any<CancellationToken>())
            .Returns((TransferPrivilege?)null);

        var act = () => _handler.Handle(Query(), CancellationToken.None);

        await act.Should().ThrowAsync<PrivilegeNotFoundException>();
    }

    [Fact(DisplayName = "非優惠持有人查詢應拋出 PrivilegeNotOwnedByCustomerException")]
    public async Task Handle_NonOwner_Throws()
    {
        _loadPrivilegePort.FindByPrivilegeIdAsync(Arg.Any<PrivilegeId>(), Arg.Any<CancellationToken>())
            .Returns(Privilege("C001"));

        var act = () => _handler.Handle(Query(customer: "C999"), CancellationToken.None);

        await act.Should().ThrowAsync<PrivilegeNotOwnedByCustomerException>();
    }
}
