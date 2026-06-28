using BankAccountQuery.Domain.Model.Shared;
using FluentAssertions;
using Xunit;

namespace BankAccountQuery.Domain.Tests.Model.Shared;

public sealed class DateRangeTests
{
    [Fact(DisplayName = "StartDate 晚於 EndDate 應拋出 ArgumentException")]
    public void Constructor_StartAfterEnd_Throws()
    {
        var act = () => new DateRange(new DateOnly(2025, 2, 1), new DateOnly(2025, 1, 1));

        act.Should().Throw<ArgumentException>().WithMessage("*StartDate*");
    }

    [Theory(DisplayName = "ExceedsMonths 應正確判斷區間月數")]
    [InlineData(2023, 12, 1, 2025, 2, 1, 13, true)]   // 14 個月 > 13
    [InlineData(2025, 1, 1, 2025, 12, 31, 13, false)] // 11 個月
    [InlineData(2024, 1, 1, 2025, 2, 1, 13, false)]   // 13 個月，不 > 13
    [InlineData(2024, 1, 1, 2025, 3, 1, 13, true)]    // 14 個月 > 13
    public void ExceedsMonths_Computation(
        int sy, int sm, int sd, int ey, int em, int ed, int months, bool expected)
    {
        var range = new DateRange(new DateOnly(sy, sm, sd), new DateOnly(ey, em, ed));

        range.ExceedsMonths(months).Should().Be(expected);
    }

    [Fact(DisplayName = "Contains 應判斷日期是否落在區間內")]
    public void Contains_Works()
    {
        var range = new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        range.Contains(new DateOnly(2025, 1, 15)).Should().BeTrue();
        range.Contains(new DateOnly(2025, 2, 1)).Should().BeFalse();
    }
}
