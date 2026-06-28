namespace BankAccountQuery.Domain.Model.Account;

/// <summary>
/// 交易通路。
/// </summary>
public enum TransactionChannel
{
    Atm,
    NetBanking,
    MobileApp,
    Counter,
    AutoTransfer
}
