using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Domain.Exceptions;
using MediatR;

namespace BankAccountQuery.Application.Queries.Account;

public sealed class GetTwdTransactionHistoryHandler
    : IRequestHandler<GetTwdTransactionHistoryQuery, TwdTransactionHistoryResult>
{
    private readonly ILoadAccountPort _loadAccountPort;
    private readonly ILoadTransactionPort _loadTransactionPort;

    public GetTwdTransactionHistoryHandler(
        ILoadAccountPort loadAccountPort,
        ILoadTransactionPort loadTransactionPort)
    {
        _loadAccountPort = loadAccountPort;
        _loadTransactionPort = loadTransactionPort;
    }

    public async Task<TwdTransactionHistoryResult> Handle(
        GetTwdTransactionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        // Step 1：透過 Output Port 取得 Aggregate
        var account = await _loadAccountPort.FindByAccountIdAsync(
            query.AccountId, cancellationToken)
            ?? throw new AccountNotFoundException(query.AccountId);

        // Step 2：委派業務規則至 Domain Model
        account.VerifyOwnership(query.CustomerId);
        account.EnsureActive();

        // Step 3：透過 Output Port 取得原始交易資料
        var rawTransactions = await _loadTransactionPort.FindByAccountIdAsync(
            query.AccountId, query.DateRange, cancellationToken);

        // Step 4：委派業務規則至 Domain Model（區間限制 + 過濾）
        var history = account.FilterByDateRange(rawTransactions, query.DateRange);

        // Step 5：轉換為 Read Model（Application Layer 職責）
        return TwdTransactionHistoryResult.From(history, query.Page, query.Size);
    }
}
