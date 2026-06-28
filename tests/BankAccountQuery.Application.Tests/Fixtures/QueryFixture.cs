using BankAccountQuery.Application.Queries.Account;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Application.Tests.Fixtures;

public static class QueryFixture
{
    public static GetTwdTransactionHistoryQuery TwdQuery(
        string customerId,
        string accountId,
        int page = 0,
        int size = 20) =>
        new(
            CustomerId.Of(customerId),
            new AccountId(accountId),
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31)),
            page,
            size);
}
