using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(Currency expected, Currency actual)
        : base($"幣別不一致：預期 [{expected}]，實際 [{actual}]") { }
}
