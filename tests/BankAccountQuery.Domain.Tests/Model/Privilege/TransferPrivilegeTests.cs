using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using Xunit;

namespace BankAccountQuery.Domain.Tests.Model.Privilege;

public sealed class TransferPrivilegeTests
{
    private static TransferPrivilege Build(
        string owner = "C001",
        int total = 10,
        int used = 3,
        DateOnly? from = null,
        DateOnly? to = null,
        IEnumerable<PrivilegeUsageRecord>? records = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new TransferPrivilege(
            PrivilegeId.Of("P001"),
            CustomerId.Of(owner),
            PrivilegeType.FeeWaiverInterBank,
            total, used,
            new DateRange(from ?? today.AddMonths(-1), to ?? today.AddMonths(1)),
            records);
    }

    [Fact(DisplayName = "剩餘次數應為總次數減已用次數")]
    public void GetRemainingQuota_ReturnsDifference()
    {
        Build(total: 10, used: 3).GetRemainingQuota().Should().Be(7);
    }

    [Fact(DisplayName = "期間內且有剩餘次數應為有效")]
    public void IsValid_WithinPeriodAndQuota_True()
    {
        Build().IsValid().Should().BeTrue();
    }

    [Fact(DisplayName = "次數用盡應為無效")]
    public void IsValid_QuotaExhausted_False()
    {
        Build(total: 5, used: 5).IsValid().Should().BeFalse();
    }

    [Fact(DisplayName = "已過期應為無效")]
    public void IsValid_Expired_False()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Build(from: today.AddMonths(-3), to: today.AddMonths(-1)).IsValid().Should().BeFalse();
    }

    [Fact(DisplayName = "非持有人驗證所有權應拋出例外")]
    public void VerifyOwnership_NonOwner_Throws()
    {
        var act = () => Build(owner: "C001").VerifyOwnership(CustomerId.Of("C999"));
        act.Should().Throw<PrivilegeNotOwnedByCustomerException>();
    }

    [Fact(DisplayName = "FilterUsageHistory 應只回傳區間內紀錄")]
    public void FilterUsageHistory_ReturnsOnlyInRange()
    {
        var records = new List<PrivilegeUsageRecord>
        {
            new("U1", new DateOnly(2025, 1, 10), Money.Twd(15m), "in"),
            new("U2", new DateOnly(2025, 3, 10), Money.Twd(15m), "out")
        };
        var privilege = Build(
            from: new DateOnly(2024, 12, 1), to: new DateOnly(2025, 12, 1),
            records: records);

        var history = privilege.FilterUsageHistory(
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31)));

        history.Count.Should().Be(1);
        history.Records[0].UsageId.Should().Be("U1");
    }
}
