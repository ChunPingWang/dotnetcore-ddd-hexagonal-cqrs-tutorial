using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Account;

/// <summary>
/// TransactionHistory — Value Object（查詢結果封裝）。
/// </summary>
public sealed record TransactionHistory(
    AccountId AccountId,
    IReadOnlyList<Transaction> Transactions,
    DateRange QueriedRange)
{
    public int Count => Transactions.Count;
}
