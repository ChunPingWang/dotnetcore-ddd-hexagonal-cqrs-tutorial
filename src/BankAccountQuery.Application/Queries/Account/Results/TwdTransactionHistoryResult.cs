using BankAccountQuery.Application.Common;
using BankAccountQuery.Domain.Model.Account;

namespace BankAccountQuery.Application.Queries.Account.Results;

/// <summary>
/// 台幣交易紀錄 Read Model。
/// </summary>
public sealed record TwdTransactionHistoryResult(
    string AccountId,
    IReadOnlyList<TwdTransactionDto> Transactions,
    PageInfo PageInfo)
{
    public static TwdTransactionHistoryResult From(
        TransactionHistory history, int page, int size)
    {
        var dtos = history.Transactions
            .Select(TwdTransactionDto.From)
            .ToList();

        return new TwdTransactionHistoryResult(
            history.AccountId.Value,
            PaginationHelper.Paginate(dtos, page, size),
            PageInfo.Of(page, size, dtos.Count));
    }
}
