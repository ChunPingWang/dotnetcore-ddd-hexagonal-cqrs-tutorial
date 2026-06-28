using System.Text.RegularExpressions;
using BankAccountQuery.Domain.Exceptions;

namespace BankAccountQuery.Domain.Model.Account;

/// <summary>
/// AccountId — 封裝帳號格式驗證（14 位數字）。
/// </summary>
public sealed partial record AccountId
{
    public string Value { get; }

    public AccountId(string value)
    {
        if (!AccountNumberPattern().IsMatch(value))
            throw new InvalidAccountIdFormatException("帳號格式不正確，需為 14 位數字");
        Value = value;
    }

    public static AccountId Of(string value) => new(value);

    public override string ToString() => Value;

    [GeneratedRegex(@"^\d{14}$")]
    private static partial Regex AccountNumberPattern();
}
