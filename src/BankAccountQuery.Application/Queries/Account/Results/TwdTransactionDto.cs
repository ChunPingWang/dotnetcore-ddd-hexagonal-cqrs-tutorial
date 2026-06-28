using BankAccountQuery.Domain.Model.Account;

namespace BankAccountQuery.Application.Queries.Account.Results;

public sealed record TwdTransactionDto(
    string TransactionId,
    string TransactionDate,
    string TransactionType,
    string Amount,
    string Description,
    string Channel)
{
    public static TwdTransactionDto From(Transaction t) => new(
        t.TransactionId.Value,
        t.TransactionDate.ToString("yyyy-MM-dd"),
        t.Type.ToString(),
        t.Amount.Amount.ToString("N2"),
        t.Description,
        t.Channel.ToString());
}
