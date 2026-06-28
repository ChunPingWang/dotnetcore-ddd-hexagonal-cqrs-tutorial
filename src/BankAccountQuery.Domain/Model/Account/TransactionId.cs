namespace BankAccountQuery.Domain.Model.Account;

/// <summary>
/// TransactionId — 交易識別碼 Value Object。
/// </summary>
public sealed record TransactionId(string Value)
{
    public string Value { get; } = string.IsNullOrWhiteSpace(Value)
        ? throw new ArgumentException("TransactionId 不可為空")
        : Value;

    public static TransactionId Of(string value) => new(value);

    public override string ToString() => Value;
}
