namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// PrivilegeId — 優惠識別碼 Value Object。
/// </summary>
public sealed record PrivilegeId(string Value)
{
    public string Value { get; } = string.IsNullOrWhiteSpace(Value)
        ? throw new ArgumentException("PrivilegeId 不可為空")
        : Value;

    public static PrivilegeId Of(string value) => new(value);

    public override string ToString() => Value;
}
