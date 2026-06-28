using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Application.Ports.Out;

/// <summary>
/// Output Port：載入 TransferPrivilege Aggregate。
/// 可有多種實作（EF Core / Redis Decorator）而 Handler 無感。
/// </summary>
public interface ILoadPrivilegePort
{
    Task<IReadOnlyList<TransferPrivilege>> FindByCustomerIdAsync(
        CustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<TransferPrivilege?> FindByPrivilegeIdAsync(
        PrivilegeId privilegeId,
        CancellationToken cancellationToken = default);
}
