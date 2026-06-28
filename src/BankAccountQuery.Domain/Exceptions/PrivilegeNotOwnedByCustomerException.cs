using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class PrivilegeNotOwnedByCustomerException : DomainException
{
    public PrivilegeNotOwnedByCustomerException(PrivilegeId privilegeId, CustomerId customerId)
        : base($"優惠 [{privilegeId.Value}] 不屬於客戶 [{customerId.Value}]") { }
}
