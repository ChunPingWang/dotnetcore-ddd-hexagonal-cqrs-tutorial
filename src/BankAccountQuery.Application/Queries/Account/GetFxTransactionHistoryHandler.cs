using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Domain.Exceptions;
using MediatR;

namespace BankAccountQuery.Application.Queries.Account;

public sealed class GetFxTransactionHistoryHandler
    : IRequestHandler<GetFxTransactionHistoryQuery, FxTransactionHistoryResult>
{
    private readonly ILoadAccountPort _loadAccountPort;
    private readonly ILoadTransactionPort _loadTransactionPort;

    public GetFxTransactionHistoryHandler(
        ILoadAccountPort loadAccountPort,
        ILoadTransactionPort loadTransactionPort)
    {
        _loadAccountPort = loadAccountPort;
        _loadTransactionPort = loadTransactionPort;
    }

    public async Task<FxTransactionHistoryResult> Handle(
        GetFxTransactionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var account = await _loadAccountPort.FindByAccountIdAsync(
            query.AccountId, cancellationToken)
            ?? throw new AccountNotFoundException(query.AccountId);

        account.VerifyOwnership(query.CustomerId);
        account.EnsureActive();

        var rawTransactions = await _loadTransactionPort.FindByAccountIdAsync(
            query.AccountId, query.DateRange, cancellationToken);

        var history = account.FilterByDateRange(rawTransactions, query.DateRange);

        return FxTransactionHistoryResult.From(
            history, query.Currency, query.Page, query.Size);
    }
}
