using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class AccountNotOwnedByCustomerException : DomainException
{
    public AccountNotOwnedByCustomerException(AccountId accountId, CustomerId customerId)
        : base($"帳戶 [{accountId.Value}] 不屬於客戶 [{customerId.Value}]") { }
}
