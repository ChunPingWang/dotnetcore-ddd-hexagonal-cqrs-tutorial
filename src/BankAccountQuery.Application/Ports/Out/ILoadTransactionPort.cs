using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Application.Ports.Out;

/// <summary>
/// Output Port：載入指定區間的原始交易資料（DB 層初步過濾）。
/// 回傳的集合僅可傳入 Domain Method（account.FilterByDateRange），
/// Handler 不得直接對此集合執行業務判斷（見 ADR-002）。
/// </summary>
public interface ILoadTransactionPort
{
    Task<IReadOnlyList<Transaction>> FindByAccountIdAsync(
        AccountId accountId,
        DateRange dateRange,
        CancellationToken cancellationToken = default);
}
