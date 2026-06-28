namespace BankAccountQuery.Domain.Model.Account;

/// <summary>
/// 帳戶狀態：啟用 / 凍結 / 結清。
/// </summary>
public enum AccountStatus
{
    Active,
    Frozen,
    Closed
}
