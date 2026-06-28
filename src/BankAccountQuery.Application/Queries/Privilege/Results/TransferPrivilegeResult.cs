using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Application.Queries.Privilege.Results;

/// <summary>
/// 轉帳優惠內容 Read Model。
/// </summary>
public sealed record TransferPrivilegeResult(
    IReadOnlyList<TransferPrivilegeDto> Privileges)
{
    public static TransferPrivilegeResult From(IReadOnlyList<TransferPrivilege> privileges) =>
        new(privileges.Select(TransferPrivilegeDto.From).ToList());
}
