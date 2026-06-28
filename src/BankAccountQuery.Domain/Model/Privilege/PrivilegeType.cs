namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// 轉帳優惠類型。
/// </summary>
public enum PrivilegeType
{
    /// <summary>免手續費跨行轉帳</summary>
    FeeWaiverInterBank,

    /// <summary>免手續費跨境匯款</summary>
    FeeWaiverCrossBorder,

    /// <summary>優惠匯率換匯</summary>
    PreferentialFxRate
}
