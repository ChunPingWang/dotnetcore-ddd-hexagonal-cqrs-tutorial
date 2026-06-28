using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;

public sealed class TransactionEntity
{
    public string TransactionId { get; set; } = default!;
    public string AccountId { get; set; } = default!;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal? TwdEquivalent { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionChannel Channel { get; set; }

    public Transaction ToDomain() =>
        new(
            new TransactionId(TransactionId),
            Type,
            new Money(Amount, Currency),
            TwdEquivalent is null ? null : Money.Twd(TwdEquivalent.Value),
            TransactionDate,
            Description,
            Channel);
}
