using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Account;

/// <summary>
/// Transaction — Entity（屬於 Account Aggregate 邊界內）。
/// </summary>
public sealed class Transaction
{
    public TransactionId TransactionId { get; }
    public TransactionType Type { get; }
    public Money Amount { get; }                 // 原幣金額
    public Money? TwdEquivalent { get; }         // 台幣等值（外幣才有）
    public DateTime TransactionDate { get; }
    public string Description { get; }
    public TransactionChannel Channel { get; }

    public Transaction(
        TransactionId transactionId,
        TransactionType type,
        Money amount,
        Money? twdEquivalent,
        DateTime transactionDate,
        string description,
        TransactionChannel channel)
    {
        TransactionId = transactionId;
        Type = type;
        Amount = amount;
        TwdEquivalent = twdEquivalent;
        TransactionDate = transactionDate;
        Description = description ?? string.Empty;
        Channel = channel;
    }

    /// <summary>
    /// 外幣交易匯率（台幣等值 / 原幣金額），原幣為 0 時回傳 0。
    /// </summary>
    public decimal? ExchangeRate =>
        TwdEquivalent is null || Amount.Amount == 0
            ? null
            : decimal.Round(TwdEquivalent.Amount / Amount.Amount, 4);
}
