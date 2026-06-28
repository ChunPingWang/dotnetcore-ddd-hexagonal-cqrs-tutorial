using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Tests.TestBuilders;

public static class TransactionTestBuilder
{
    public static Transaction On(DateTime date) =>
        new(TransactionId.Of($"T-{date:yyyyMMddHHmmssfff}"),
            TransactionType.Credit,
            Money.Twd(100m),
            twdEquivalent: null,
            date,
            "測試交易",
            TransactionChannel.Atm);

    public static IReadOnlyList<Transaction> SampleList() => new List<Transaction>
    {
        On(new DateTime(2025, 1, 5)),
        On(new DateTime(2025, 1, 10)),
        On(new DateTime(2025, 1, 20))
    };
}
