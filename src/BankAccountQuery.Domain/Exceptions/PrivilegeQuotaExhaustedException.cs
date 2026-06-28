using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Domain.Exceptions;

public sealed class PrivilegeQuotaExhaustedException : DomainException
{
    public PrivilegeQuotaExhaustedException(PrivilegeId privilegeId)
        : base($"優惠 [{privilegeId.Value}] 使用次數已用盡") { }
}
