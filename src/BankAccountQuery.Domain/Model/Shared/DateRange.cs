namespace BankAccountQuery.Domain.Model.Shared;

/// <summary>
/// DateRange — 封裝日期區間驗證與業務語意。
/// </summary>
public sealed record DateRange(DateOnly StartDate, DateOnly EndDate)
{
    public DateOnly EndDate { get; } = StartDate <= EndDate
        ? EndDate
        : throw new ArgumentException("StartDate 不可晚於 EndDate");

    /// <summary>
    /// 區間是否超過指定月數（以年月差計算，與日無關）。
    /// </summary>
    public bool ExceedsMonths(int months) =>
        ((EndDate.Year - StartDate.Year) * 12 + EndDate.Month - StartDate.Month) > months;

    public bool Contains(DateOnly date) =>
        date >= StartDate && date <= EndDate;
}
