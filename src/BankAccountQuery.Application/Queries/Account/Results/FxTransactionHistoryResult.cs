using BankAccountQuery.Application.Common;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Application.Queries.Account.Results;

/// <summary>
/// 外幣交易紀錄 Read Model。
/// </summary>
public sealed record FxTransactionHistoryResult(
    string AccountId,
    string CurrencyCode,
    IReadOnlyList<FxTransactionDto> Transactions,
    PageInfo PageInfo)
{
    public static FxTransactionHistoryResult From(
        TransactionHistory history, Currency currency, int page, int size)
    {
        var dtos = history.Transactions
            .Where(t => t.Amount.Currency == currency)
            .Select(FxTransactionDto.From)
            .ToList();

        return new FxTransactionHistoryResult(
            history.AccountId.Value,
            currency.ToString(),
            PaginationHelper.Paginate(dtos, page, size),
            PageInfo.Of(page, size, dtos.Count));
    }
}
