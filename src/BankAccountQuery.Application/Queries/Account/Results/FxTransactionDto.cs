using BankAccountQuery.Domain.Model.Account;

namespace BankAccountQuery.Application.Queries.Account.Results;

public sealed record FxTransactionDto(
    string TransactionId,
    string TransactionDate,
    string TransactionType,
    string CurrencyCode,
    string FxAmount,
    string TwdEquivalent,
    string ExchangeRate,
    string Description)
{
    public static FxTransactionDto From(Transaction t) => new(
        t.TransactionId.Value,
        t.TransactionDate.ToString("yyyy-MM-dd"),
        t.Type.ToString(),
        t.Amount.Currency.ToString(),
        t.Amount.Amount.ToString("N2"),
        t.TwdEquivalent?.Amount.ToString("N2") ?? "-",
        t.ExchangeRate?.ToString("N4") ?? "-",
        t.Description);
}
