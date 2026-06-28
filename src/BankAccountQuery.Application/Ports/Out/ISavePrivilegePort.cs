using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Application.Ports.Out;

/// <summary>
/// Output Port（寫入側）：以聚合根為單位持久化 TransferPrivilege。
/// 符合 ADR-001：寫入只透過聚合根，確保不變量有守門員。
/// </summary>
public interface ISavePrivilegePort
{
    Task SaveAsync(TransferPrivilege privilege, CancellationToken cancellationToken = default);
}
