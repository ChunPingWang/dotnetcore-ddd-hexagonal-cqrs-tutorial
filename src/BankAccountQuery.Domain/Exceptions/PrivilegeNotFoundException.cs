using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class PrivilegeNotFoundException : DomainException
{
    public PrivilegeNotFoundException(PrivilegeId privilegeId)
        : base($"優惠 [{privilegeId.Value}] 不存在") { }
}
