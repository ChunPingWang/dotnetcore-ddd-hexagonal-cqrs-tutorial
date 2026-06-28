using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Application.Ports.Out;

/// <summary>
/// Output Port：載入 Account Aggregate（不含交易明細）。
/// 由 Infrastructure 的 Driven Adapter 實作。
/// </summary>
public interface ILoadAccountPort
{
    Task<Account?> FindByAccountIdAsync(
        AccountId accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> FindAllByCustomerIdAsync(
        CustomerId customerId,
        CancellationToken cancellationToken = default);
}
