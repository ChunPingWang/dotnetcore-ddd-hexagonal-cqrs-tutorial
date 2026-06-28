using BankAccountQuery.Domain.Exceptions;

namespace BankAccountQuery.Domain.Model.Shared;

/// <summary>
/// Money — 不可變 Value Object，封裝金額與幣別業務語意。
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("金額不可為負數", nameof(amount));
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("金額最多 2 位小數", nameof(amount));
        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
        return new Money(Amount + other.Amount, Currency);
    }

    public static Money Twd(decimal amount) => new(amount, Currency.TWD);

    public static Money Of(decimal amount, Currency currency) => new(amount, currency);

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount:N2} {Currency}";

    public static bool operator ==(Money? left, Money? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Money? left, Money? right) => !(left == right);
}
