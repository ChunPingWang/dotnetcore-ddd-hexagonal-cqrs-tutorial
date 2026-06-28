using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using Xunit;

namespace BankAccountQuery.Domain.Tests.Model.Privilege;

public sealed class TransferPrivilegeUseTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static TransferPrivilege Build(
        int total = 5, int used = 0, DateOnly? from = null, DateOnly? to = null) =>
        new(PrivilegeId.Of("P010"), CustomerId.Of("C002"),
            PrivilegeType.FeeWaiverInterBank, total, used,
            new DateRange(from ?? Today.AddYears(-1), to ?? Today.AddYears(1)));

    [Fact(DisplayName = "使用一次優惠應加計次數並回傳使用紀錄")]
    public void Use_Valid_IncrementsUsedQuota()
    {
        var privilege = Build(total: 5, used: 0);

        var record = privilege.Use("U-1", Money.Twd(15m), "跨行轉帳免手續費", Today);

        privilege.UsedQuota.Should().Be(1);
        privilege.GetRemainingQuota().Should().Be(4);
        privilege.UsageRecords.Should().ContainSingle(r => r.UsageId == "U-1");
        record.SavedAmount.Should().Be(Money.Twd(15m));
    }

    [Fact(DisplayName = "使用一次優惠應發布 TransferPrivilegeUsedEvent")]
    public void Use_Valid_RaisesDomainEvent()
    {
        var privilege = Build(total: 5, used: 2);

        privilege.Use("U-9", Money.Twd(30m), "x", Today);

        privilege.DomainEvents.Should().ContainSingle();
        var evt = privilege.DomainEvents[0].Should().BeOfType<TransferPrivilegeUsedEvent>().Subject;
        evt.PrivilegeId.Value.Should().Be("P010");
        evt.OwnerId.Value.Should().Be("C002");
        evt.UsageId.Should().Be("U-9");
        evt.RemainingQuota.Should().Be(2); // 5 - (2+1)
    }

    [Fact(DisplayName = "次數用盡時使用應拋出 PrivilegeQuotaExhaustedException 且狀態不變")]
    public void Use_QuotaExhausted_ThrowsAndDoesNotMutate()
    {
        var privilege = Build(total: 1, used: 1);

        var act = () => privilege.Use("U-X", Money.Twd(15m), "x", Today);

        act.Should().Throw<PrivilegeQuotaExhaustedException>();
        privilege.UsedQuota.Should().Be(1);
        privilege.UsageRecords.Should().BeEmpty();
        privilege.DomainEvents.Should().BeEmpty();
    }

    [Fact(DisplayName = "已過期時使用應拋出 PrivilegeExpiredException 且狀態不變")]
    public void Use_Expired_ThrowsAndDoesNotMutate()
    {
        var privilege = Build(
            total: 5, used: 0,
            from: new DateOnly(2020, 1, 1), to: new DateOnly(2020, 12, 31));

        var act = () => privilege.Use("U-X", Money.Twd(15m), "x", Today);

        act.Should().Throw<PrivilegeExpiredException>();
        privilege.UsedQuota.Should().Be(0);
        privilege.DomainEvents.Should().BeEmpty();
    }

    [Fact(DisplayName = "ClearDomainEvents 應清空已發布事件")]
    public void ClearDomainEvents_RemovesAll()
    {
        var privilege = Build();
        privilege.Use("U-1", Money.Twd(15m), "x", Today);

        privilege.ClearDomainEvents();

        privilege.DomainEvents.Should().BeEmpty();
    }
}
