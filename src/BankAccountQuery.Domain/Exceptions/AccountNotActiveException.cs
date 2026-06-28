using BankAccountQuery.Domain.Model.Account;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class AccountNotActiveException : DomainException
{
    public AccountNotActiveException(AccountId accountId, AccountStatus status)
        : base($"帳戶 [{accountId.Value}] 狀態為 [{status}]，無法查詢") { }
}
