using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using Xunit;

namespace BankAccountQuery.Domain.Tests.Model.Privilege;

public sealed class TransferPrivilegeEventSourcingTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact(DisplayName = "Grant 應產生創生事件並設定初始狀態，Version=1")]
    public void Grant_RaisesGrantedEvent()
    {
        var p = TransferPrivilege.Grant(
            PrivilegeId.Of("P010"), CustomerId.Of("C002"),
            PrivilegeType.FeeWaiverInterBank, 5,
            new DateRange(Today.AddYears(-1), Today.AddYears(1)));

        p.TotalQuota.Should().Be(5);
        p.UsedQuota.Should().Be(0);
        p.Version.Should().Be(1);
        p.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransferPrivilegeGrantedEvent>();
    }

    [Fact(DisplayName = "以事件序列重播應重建出正確狀態")]
    public void Load_ReplaysToCorrectState()
    {
        var history = new IDomainEvent[]
        {
            new TransferPrivilegeGrantedEvent(
                PrivilegeId.Of("P001"), CustomerId.Of("C001"),
                PrivilegeType.FeeWaiverInterBank, 10,
                new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), DateTime.UtcNow),
            new TransferPrivilegeUsedEvent(
                PrivilegeId.Of("P001"), CustomerId.Of("C001"), "U001",
                Money.Twd(15m), "跨行轉帳免手續費", 9, new DateOnly(2025, 1, 12), DateTime.UtcNow),
            new TransferPrivilegeUsedEvent(
                PrivilegeId.Of("P001"), CustomerId.Of("C001"), "U002",
                Money.Twd(30m), "跨行轉帳免手續費", 8, new DateOnly(2025, 2, 3), DateTime.UtcNow)
        };

        var p = TransferPrivilege.Load(history);

        p.PrivilegeId.Value.Should().Be("P001");
        p.OwnerId.Value.Should().Be("C001");
        p.UsedQuota.Should().Be(2);
        p.GetRemainingQuota().Should().Be(8);
        p.UsageRecords.Should().HaveCount(2);
        p.Version.Should().Be(3);              // 1 granted + 2 used
        p.DomainEvents.Should().BeEmpty();     // 重播不重新產生事件
    }

    [Fact(DisplayName = "重播後再使用：產生新事件且 Version 遞增")]
    public void Load_ThenUse_AppendsNewEvent()
    {
        var p = TransferPrivilege.Load(new IDomainEvent[]
        {
            new TransferPrivilegeGrantedEvent(
                PrivilegeId.Of("P010"), CustomerId.Of("C002"),
                PrivilegeType.FeeWaiverInterBank, 5,
                Today.AddYears(-1), Today.AddYears(1), DateTime.UtcNow)
        });
        p.Version.Should().Be(1);

        p.Use("U-9", Money.Twd(15m), "x", Today);

        p.UsedQuota.Should().Be(1);
        p.Version.Should().Be(2);
        p.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransferPrivilegeUsedEvent>();
    }

    [Fact(DisplayName = "重播出已過期的優惠，使用時仍應拋出例外（不變量不因來源而異）")]
    public void Load_Expired_ThenUse_Throws()
    {
        var p = TransferPrivilege.Load(new IDomainEvent[]
        {
            new TransferPrivilegeGrantedEvent(
                PrivilegeId.Of("P013"), CustomerId.Of("C002"),
                PrivilegeType.FeeWaiverInterBank, 5,
                new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31), DateTime.UtcNow)
        });

        var act = () => p.Use("U-X", Money.Twd(15m), "x", Today);

        act.Should().Throw<PrivilegeExpiredException>();
    }
}
