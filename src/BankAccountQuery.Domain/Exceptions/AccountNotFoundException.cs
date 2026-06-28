using BankAccountQuery.Domain.Model.Account;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(AccountId accountId)
        : base($"帳戶 [{accountId.Value}] 不存在") { }
}
