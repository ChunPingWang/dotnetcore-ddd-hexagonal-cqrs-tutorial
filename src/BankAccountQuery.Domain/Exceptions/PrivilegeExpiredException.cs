using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class PrivilegeExpiredException : DomainException
{
    public PrivilegeExpiredException(PrivilegeId privilegeId)
        : base($"優惠 [{privilegeId.Value}] 不在有效期間內，無法使用") { }
}
