namespace BankAccountQuery.Domain.Model.Shared;

/// <summary>
/// CustomerId — 強型別，防止 Primitive Obsession。
/// </summary>
public sealed record CustomerId(string Value)
{
    public string Value { get; } = string.IsNullOrWhiteSpace(Value)
        ? throw new ArgumentException("CustomerId 不可為空")
        : Value;

    public static CustomerId Of(string value) => new(value);

    public override string ToString() => Value;
}
